using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Agents;

public class NutritionAgent : IStreamingSubAgent
{
    private readonly INutritionTools _nutritionTools;
    private readonly IModelRequestContextAccessor _requestContextAccessor;
    private readonly AIAgent _agent;

    public string Name => "nutrition";

    public AIAgent Agent => _agent;

    public NutritionAgent(IChatClient chatClient, INutritionTools nutritionTools, IModelRequestContextAccessor requestContextAccessor)
    {
        _nutritionTools = nutritionTools;
        _requestContextAccessor = requestContextAccessor;
        _agent = chatClient.AsAIAgent(
            "nutrition-agent",
            "FitTrack nutrition expert",
            """
            You are FitTrack's nutrition specialist.
            Use tools for factual diet analysis and food search.
            Keep answers practical, coach-like, and concise.
            Ask a follow-up question when meal details or goals are missing.
            """,
            BuildTools());
    }

    [Description("Answer a nutrition question with the user's recent context")]
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

        return result ?? new AgentExecutionResult("NutritionAgent", "No response.", ToolEvents: ["subagent:nutrition"]);
    }

    public async IAsyncEnumerable<CoachStreamEvent> ExecuteStreamingAsync(
        string userId,
        IReadOnlyList<ConversationMessage> history,
        string prompt,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return CoachStreamEvent.ToolEvent("subagent:nutrition");

        var responseText = new StringBuilder();
        using var _ = _requestContextAccessor.BeginScope(context =>
        {
            context.UserId = userId;
            context.RequestType = ModelRequestType.NutritionAgent;
            context.RequestSummary = BuildSummary(prompt, "nutrition chat");
            context.ToolEvents = ["subagent:nutrition"];
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

        var snapshot = await _nutritionTools.BuildDailyNutritionSnapshotAsync(userId, ct);
        yield return CoachStreamEvent.Completed(new AgentExecutionResult(
            "NutritionAgent",
            responseText.Length == 0 ? "No response." : responseText.ToString(),
            Snapshot: snapshot,
            ToolEvents: ["subagent:nutrition"]));
    }

    private IList<AITool> BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)SearchUsdaAsync,
                "search_usda_food",
                "Search USDA for a specific food and return a concise nutrition reference."),
            AIFunctionFactory.Create(
                (Func<string, CancellationToken, Task<string>>)AnalyzeMealAsync,
                "analyze_meal",
                "Analyze a described meal and estimate macros.")
        ];
    }

    private Task<string> SearchUsdaAsync(string query, CancellationToken ct) => _nutritionTools.SearchUsdaAsync(query, ct);

    private Task<string> AnalyzeMealAsync(string prompt, CancellationToken ct)
    {
        var userId = ExtractUserId(prompt);
        var normalizedPrompt = RemoveUserIdPrefix(prompt);
        return _nutritionTools.AnalyzeMealAsync(userId, normalizedPrompt, ct);
    }

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
