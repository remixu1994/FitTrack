namespace FitTrack.Copilot.Service;

public interface ICoachChatService
{
    IAsyncEnumerable<CoachStreamEvent> SendStreamingAsync(string userId, string threadId, string? text, string? imageDataUrl, CancellationToken ct = default);
}
