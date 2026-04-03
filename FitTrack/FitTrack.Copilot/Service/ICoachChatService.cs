namespace FitTrack.Copilot.Service;

public interface ICoachChatService
{
    Task<AgentExecutionResult> SendAsync(string userId, string threadId, string? text, string? imageDataUrl, CancellationToken ct = default);
}
