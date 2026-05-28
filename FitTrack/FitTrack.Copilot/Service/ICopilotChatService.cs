using FitTrack.Copilot.Abstractions.Models;

namespace FitTrack.Copilot.Service;

public interface ICopilotChatService
{
    Task<CopilotChatResponse> SendAsync(CopilotChatRequest request, CancellationToken ct = default);
}

public sealed class CopilotChatRequest
{
    public string? Text { get; set; }
    public string? ImageDataUrl { get; set; }
    public string? UserId { get; set; }
}

public sealed class CopilotChatResponse
{
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NutritionResult? Nutrition { get; set; }
}
