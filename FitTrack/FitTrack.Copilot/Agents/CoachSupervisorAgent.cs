using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

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

    public async IAsyncEnumerable<CoachStreamEvent> SendStreamingAsync(
        string userId,
        string threadId,
        string? text,
        string? imageDataUrl,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var chatClient = await _chatClientFactory.CreateAsync(userId, ct);
        var history = await _conversationMemory.GetRecentMessagesAsync(threadId, 8, ct);
        var prompt = text?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(imageDataUrl))
        {
            var agent = new VisionNutritionAgent(
                _serviceProvider.GetRequiredService<IVisionTools>());
            var result = await agent.RunAsync(userId, prompt, imageDataUrl, ct);
            await foreach (var update in StreamImmediateResult(result, ct))
            {
                yield return update;
            }

            yield break;
        }

        var wantsProgress = ContainsAny(prompt, "progress", "check-in", "summary", "weight", "weekly", "month");
        var wantsWorkout = ContainsAny(prompt, "workout", "training", "exercise", "plan", "gym", "session");
        var wantsNutrition = ContainsAny(prompt, "meal", "food", "diet", "calorie", "protein", "carb", "macro", "nutrition");

        if (wantsProgress && !wantsWorkout && !wantsNutrition)
        {
            var progressAgent = new ProgressCheckInAgent(
                _serviceProvider.GetRequiredService<IProgressTools>());
            var result = await progressAgent.RunAsync(userId, ct);
            await foreach (var update in StreamImmediateResult(result, ct))
            {
                yield return update;
            }

            yield break;
        }

        if (wantsWorkout && wantsNutrition)
        {
            await foreach (var update in RunWorkflowStreamingAsync(
                               "CoachSupervisorAgent",
                               userId,
                               history,
                               prompt,
                               ct,
                               new WorkoutAgent(chatClient, _serviceProvider.GetRequiredService<IWorkoutTools>()),
                               new NutritionAgent(chatClient, _serviceProvider.GetRequiredService<INutritionTools>())))
            {
                yield return update;
            }

            yield break;
        }

        if (wantsWorkout)
        {
            await foreach (var update in RunWorkflowStreamingAsync(
                               "CoachSupervisorAgent",
                               userId,
                               history,
                               prompt,
                               ct,
                               new WorkoutAgent(chatClient, _serviceProvider.GetRequiredService<IWorkoutTools>())))
            {
                yield return update;
            }

            yield break;
        }

        if (wantsProgress)
        {
            var progressAgent = new ProgressCheckInAgent(
                _serviceProvider.GetRequiredService<IProgressTools>());
            var result = await progressAgent.RunAsync(userId, ct);
            await foreach (var update in StreamImmediateResult(result, ct))
            {
                yield return update;
            }

            yield break;
        }

        await foreach (var update in RunWorkflowStreamingAsync(
                           "CoachSupervisorAgent",
                           userId,
                           history,
                           string.IsNullOrWhiteSpace(prompt) ? "Help me with my diet today." : prompt,
                           ct,
                           new NutritionAgent(chatClient, _serviceProvider.GetRequiredService<INutritionTools>())))
        {
            yield return update;
        }
    }

    private static bool ContainsAny(string prompt, params string[] words)
        => words.Any(word => prompt.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> MergeEvents(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        => (left ?? Array.Empty<string>()).Concat(right ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static async IAsyncEnumerable<CoachStreamEvent> StreamImmediateResult(
        AgentExecutionResult result,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (result.ToolEvents is not null)
        {
            foreach (var toolEvent in result.ToolEvents)
            {
                ct.ThrowIfCancellationRequested();
                yield return CoachStreamEvent.ToolEvent(toolEvent);
            }
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            yield return CoachStreamEvent.Token(result.Message);
        }

        yield return CoachStreamEvent.Completed(result);
    }

    private async IAsyncEnumerable<CoachStreamEvent> RunWorkflowStreamingAsync(
        string agentName,
        string userId,
        IReadOnlyList<ConversationMessage> history,
        string prompt,
        [EnumeratorCancellation] CancellationToken ct,
        params IAgentSubAgent[] subAgents)
    {
        var results = new List<AgentExecutionResult>();
        var emittedAnyText = false;

        foreach (var subAgent in subAgents)
        {
            var emittedTextForSubAgent = false;

            if (subAgent is IStreamingSubAgent streamingSubAgent)
            {
                await foreach (var update in streamingSubAgent.ExecuteStreamingAsync(userId, history, prompt, ct).WithCancellation(ct))
                {
                    if (update.Type == CoachStreamEventType.Token && !string.IsNullOrWhiteSpace(update.Value))
                    {
                        if (emittedAnyText && !emittedTextForSubAgent)
                        {
                            yield return CoachStreamEvent.Token("\n\n");
                        }

                        emittedAnyText = true;
                        emittedTextForSubAgent = true;
                        yield return update;
                        continue;
                    }

                    if (update.Type == CoachStreamEventType.Completed && update.Result is not null)
                    {
                        results.Add(update.Result);
                        continue;
                    }

                    yield return update;
                }

                continue;
            }

            var result = await subAgent.ExecuteAsync(userId, history, prompt, ct);
            if (result.ToolEvents is not null)
            {
                foreach (var toolEvent in result.ToolEvents)
                {
                    yield return CoachStreamEvent.ToolEvent(toolEvent);
                }
            }

            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                if (emittedAnyText)
                {
                    yield return CoachStreamEvent.Token("\n\n");
                }

                yield return CoachStreamEvent.Token(result.Message);
                emittedAnyText = true;
            }

            results.Add(result);
        }

        yield return CoachStreamEvent.Completed(CombineResults(agentName, results));
    }

    private static AgentExecutionResult CombineResults(string agentName, IReadOnlyList<AgentExecutionResult> results)
    {
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
