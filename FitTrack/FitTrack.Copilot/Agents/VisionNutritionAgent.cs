using FitTrack.Copilot.Service;
namespace FitTrack.Copilot.Agents;

public class VisionNutritionAgent
{
    private readonly IVisionTools _visionTools;

    public VisionNutritionAgent(IVisionTools visionTools)
    {
        _visionTools = visionTools;
    }

    public async Task<AgentExecutionResult> RunAsync(string userId, string? text, string imageDataUrl, string? languageCode, CancellationToken ct = default)
    {
        var result = await _visionTools.AnalyzeImageAsync(userId, text, imageDataUrl, languageCode, ct);
        return new AgentExecutionResult(
            "VisionNutritionAgent",
            result.Summary,
            result.StructuredPayload,
            result.Snapshot,
            new[] { "subagent:vision", "vision.analyze_image" });
    }
}
