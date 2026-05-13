using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Agents;

public class WorkoutAgent : IStreamingSubAgent
{
    private readonly IWorkoutTools _workoutTools;
    private readonly IModelRequestContextAccessor _requestContextAccessor;
    private readonly AIAgent _agent;

    public string Name => "workout";

    public AIAgent Agent => _agent;

    public WorkoutAgent(IChatClient chatClient, IWorkoutTools workoutTools, IModelRequestContextAccessor requestContextAccessor)
    {
        _workoutTools = workoutTools;
        _requestContextAccessor = requestContextAccessor;
        _agent = chatClient.AsAIAgent(
            "workout-agent",
            "FitTrack workout expert",
            """
            You are FitTrack's workout programming specialist.
            Focus on training plans, exercise selection, and session review.
            Use tools for user-specific history and plan suggestions.
            Keep the response grounded and executable.
            """,
            BuildTools());
    }

    [Description("Answer training and workout questions")]
    public async Task<AgentExecutionResult> ExecuteAsync(string userId, IReadOnlyList<ConversationMessage> history, string prompt, CancellationToken ct = default)
    {
        AgentExecutionResult? result = null;
        await foreach (var update in ExecuteStreamingAsync(userId, history, prompt, ct).WithCancellation(ct))
        {
            if (update.Type == CoachStreamEventType.Completed)
            {
                result = update.Result;
            }
        }

        return result ?? new AgentExecutionResult("WorkoutAgent", "No response.", ToolEvents: ["subagent:workout"]);
    }

    public async IAsyncEnumerable<CoachStreamEvent> ExecuteStreamingAsync(
        string userId,
        IReadOnlyList<ConversationMessage> history,
        string prompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return CoachStreamEvent.ToolEvent("subagent:workout");

        var responseText = new StringBuilder();
        using var _ = _requestContextAccessor.BeginScope(context =>
        {
            context.UserId = userId;
            context.RequestType = ModelRequestType.WorkoutAgent;
            context.RequestSummary = BuildSummary(prompt, "workout plan");
            context.ToolEvents = ["subagent:workout"];
        });

        await foreach (var update in _agent.RunStreamingAsync(
                           new[]
                           {
                               new ChatMessage(ChatRole.User, BuildPrompt(history, prompt, userId))
                           },
                           cancellationToken: ct).WithCancellation(ct))
        {
            if (string.IsNullOrWhiteSpace(update.Text))
            {
                continue;
            }

            responseText.Append(update.Text);
            yield return CoachStreamEvent.Token(update.Text);
        }

        yield return CoachStreamEvent.Completed(new AgentExecutionResult(
            "WorkoutAgent",
            responseText.Length == 0 ? "No response." : responseText.ToString(),
            ToolEvents: ["subagent:workout"]));
    }

    private IList<AITool> BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)SuggestWorkoutPlanAsync,
                "suggest_workout_plan",
                "Suggest or refine a workout plan based on the user request."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)SummarizeWorkoutHistoryAsync,
                "summarize_workout_history",
                "Summarize recent workout history for the current user.")
        ];
    }

    private Task<string> SuggestWorkoutPlanAsync(string prompt, CancellationToken ct)
    {
        var userId = ExtractUserId(prompt);
        var normalizedPrompt = RemoveUserIdPrefix(prompt);
        return _workoutTools.SuggestWorkoutPlanAsync(userId, normalizedPrompt, ct);
    }

    private Task<string> SummarizeWorkoutHistoryAsync(string prompt, CancellationToken ct)
        => _workoutTools.SummarizeWorkoutHistoryAsync(ExtractUserId(prompt), ct);

    private static string BuildPrompt(IReadOnlyList<ConversationMessage> history, string prompt, string userId)
    {
        var recent = string.Join('\n', history.TakeLast(6).Select(m => $"{m.Role}: {m.ContentText ?? m.ContentJson}"));
        return $"UserId:{userId}\nRecent conversation:\n{recent}\n\nUser request:\n{prompt}";
    }

    private static string ExtractUserId(string prompt)
        => prompt.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith("UserId:", StringComparison.OrdinalIgnoreCase))?
            .Split(':', 2)[1]
            .Trim() ?? "anonymous";

    private static string RemoveUserIdPrefix(string prompt)
        => string.Join('\n', prompt.Split('\n').Where(line => !line.StartsWith("UserId:", StringComparison.OrdinalIgnoreCase)));

    private static string BuildSummary(string prompt, string fallback)
    {
        var normalized = RemoveUserIdPrefix(prompt).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        return normalized.Length <= 200 ? normalized : normalized[..200];
    }
}
