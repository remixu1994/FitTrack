namespace FitTrack.Copilot.Service;

public interface ICoachChatService
{
    IAsyncEnumerable<CoachStreamEvent> SendStreamingAsync(string userId, string threadId, string? text, string? imageDataUrl, string? languageCode, CancellationToken ct = default);
}
