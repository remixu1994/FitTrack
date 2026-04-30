using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Agents;

public class CoachSupervisorAgent : ICoachChatService
{
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly IConversationMemory _conversationMemory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CoachSupervisorAgent> _logger;

    public CoachSupervisorAgent(
        IAIChatClientFactory chatClientFactory,
        IConversationMemory conversationMemory,
        IServiceProvider serviceProvider,
        ILogger<CoachSupervisorAgent> logger)
    {
        _chatClientFactory = chatClientFactory;
        _conversationMemory = conversationMemory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> SendAsync(string userId, string threadId, string? text, string? imageDataUrl, CancellationToken ct = default)
    {
        var chatClient = await _chatClientFactory.CreateAsync(userId, ct);
        var history = await _conversationMemory.GetRecentMessagesAsync(threadId, 8, ct);
        var prompt = text?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(imageDataUrl))
        {
            var agent = new VisionNutritionAgent(
                _serviceProvider.GetRequiredService<IVisionTools>());
            return await agent.RunAsync(userId, prompt, imageDataUrl, ct);
        }

        var wantsProgress = ContainsAny(prompt, "progress", "check-in", "summary", "weight", "weekly", "month");
        var wantsWorkout = ContainsAny(prompt, "workout", "training", "exercise", "plan", "gym", "session");
        var wantsNutrition = ContainsAny(prompt, "meal", "food", "diet", "calorie", "protein", "carb", "macro", "nutrition");

        if (wantsProgress && !wantsWorkout && !wantsNutrition)
        {
            var progressAgent = new ProgressCheckInAgent(
                _serviceProvider.GetRequiredService<IProgressTools>());
            return await progressAgent.RunAsync(userId, ct);
        }

        if (wantsWorkout && wantsNutrition)
        {
            return await RunWorkflowAsync(
                chatClient,
                "CoachSupervisorAgent",
                userId,
                history,
                prompt,
                ct,
                new WorkoutAgent(chatClient, _serviceProvider.GetRequiredService<IWorkoutTools>()),
                new NutritionAgent(chatClient, _serviceProvider.GetRequiredService<INutritionTools>()));
        }

        if (wantsWorkout)
        {
            return await RunWorkflowAsync(
                chatClient,
                "CoachSupervisorAgent",
                userId,
                history,
                prompt,
                ct,
                new WorkoutAgent(chatClient, _serviceProvider.GetRequiredService<IWorkoutTools>()));
        }

        if (wantsProgress)
        {
            var progressAgent = new ProgressCheckInAgent(
                _serviceProvider.GetRequiredService<IProgressTools>());
            return await progressAgent.RunAsync(userId, ct);
        }

        return await RunWorkflowAsync(
            chatClient,
            "CoachSupervisorAgent",
            userId,
            history,
            string.IsNullOrWhiteSpace(prompt) ? "Help me with my diet today." : prompt,
            ct,
            new NutritionAgent(chatClient, _serviceProvider.GetRequiredService<INutritionTools>()));
    }

    private static bool ContainsAny(string prompt, params string[] words)
        => words.Any(word => prompt.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> MergeEvents(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        => (left ?? Array.Empty<string>()).Concat(right ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private async Task<AgentExecutionResult> RunWorkflowAsync(
        IChatClient chatClient,
        string agentName,
        string userId,
        IReadOnlyList<ConversationMessage> history,
        string prompt,
        CancellationToken ct,
        params IAgentSubAgent[] subAgents)
    {
        var results = new List<AgentExecutionResult>();
        foreach (var subAgent in subAgents)
        {
            results.Add(await subAgent.ExecuteAsync(userId, history, prompt, ct));
        }

        var finalMessage = string.Join(
            "\n\n",
            results
                .Select(result => result.Message?.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(finalMessage))
        {
            finalMessage = "No workflow response.";
        }

        var snapshot = results.LastOrDefault(result => result.Snapshot is not null)?.Snapshot;
        var structuredPayloads = results
            .Where(result => result.StructuredPayload is not null)
            .Select(result => new KeyValuePair<string, Dictionary<string, object?>>(result.AgentName, result.StructuredPayload!))
            .ToList();

        Dictionary<string, object?>? structuredPayload = structuredPayloads.Count switch
        {
            0 => null,
            1 => structuredPayloads[0].Value,
            _ => new Dictionary<string, object?>
            {
                ["subagents"] = structuredPayloads.ToDictionary(item => item.Key, item => (object?)item.Value)
            }
        };

        var toolEvents = results.Aggregate(
            new List<string>(),
            (events, result) => MergeEvents(events, result.ToolEvents).ToList());

        return new AgentExecutionResult(
            agentName,
            finalMessage,
            structuredPayload,
            snapshot,
            toolEvents);
    }
}
