using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Agents;

public class CoachSupervisorAgent : ICoachChatService
{
    private readonly NutritionAgent _nutritionAgent;
    private readonly WorkoutAgent _workoutAgent;
    private readonly VisionNutritionAgent _visionNutritionAgent;
    private readonly ProgressCheckInAgent _progressCheckInAgent;
    private readonly IConversationMemory _conversationMemory;

    public CoachSupervisorAgent(
        NutritionAgent nutritionAgent,
        WorkoutAgent workoutAgent,
        VisionNutritionAgent visionNutritionAgent,
        ProgressCheckInAgent progressCheckInAgent,
        IConversationMemory conversationMemory)
    {
        _nutritionAgent = nutritionAgent;
        _workoutAgent = workoutAgent;
        _visionNutritionAgent = visionNutritionAgent;
        _progressCheckInAgent = progressCheckInAgent;
        _conversationMemory = conversationMemory;
    }

    public async Task<AgentExecutionResult> SendAsync(string userId, string threadId, string? text, string? imageDataUrl, CancellationToken ct = default)
    {
        var history = await _conversationMemory.GetRecentMessagesAsync(threadId, 8, ct);
        var prompt = text?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(imageDataUrl))
        {
            return await _visionNutritionAgent.RunAsync(userId, prompt, imageDataUrl, ct);
        }

        var wantsProgress = ContainsAny(prompt, "progress", "check-in", "summary", "weight", "weekly", "month");
        var wantsWorkout = ContainsAny(prompt, "workout", "training", "exercise", "plan", "gym", "session");
        var wantsNutrition = ContainsAny(prompt, "meal", "food", "diet", "calorie", "protein", "carb", "macro", "nutrition");

        if (wantsProgress && !wantsWorkout && !wantsNutrition)
        {
            return await _progressCheckInAgent.RunAsync(userId, ct);
        }

        if (wantsWorkout && wantsNutrition)
        {
            return await RunWorkflowAsync(
                "CoachSupervisorAgent",
                userId,
                history,
                prompt,
                ct,
                _workoutAgent,
                _nutritionAgent);
        }

        if (wantsWorkout)
        {
            return await RunWorkflowAsync(
                "CoachSupervisorAgent",
                userId,
                history,
                prompt,
                ct,
                _workoutAgent);
        }

        if (wantsProgress)
        {
            return await _progressCheckInAgent.RunAsync(userId, ct);
        }

        return await RunWorkflowAsync(
            "CoachSupervisorAgent",
            userId,
            history,
            string.IsNullOrWhiteSpace(prompt) ? "Help me with my diet today." : prompt,
            ct,
            _nutritionAgent);
    }

    private static bool ContainsAny(string prompt, params string[] words)
        => words.Any(word => prompt.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> MergeEvents(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        => (left ?? Array.Empty<string>()).Concat(right ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private async Task<AgentExecutionResult> RunWorkflowAsync(
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
