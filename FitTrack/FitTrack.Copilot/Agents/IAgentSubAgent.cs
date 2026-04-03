using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.Agents.AI;

namespace FitTrack.Copilot.Agents;

public interface IAgentSubAgent
{
    string Name { get; }

    AIAgent Agent { get; }

    Task<AgentExecutionResult> ExecuteAsync(string userId, IReadOnlyList<ConversationMessage> history, string prompt, CancellationToken ct = default);
}
