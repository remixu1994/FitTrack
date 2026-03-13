using System.Text.RegularExpressions;
using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.SemanticKernel.Orchestrator;

namespace FitTrack.Copilot.Service;

public class FoodAiService : IFoodAiService
{
    private readonly FoodNutritionOrchestrator _orchestrator;

    public FoodAiService(FoodNutritionOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<NutritionResult> AnalyzeAsync(FoodRequest req, CancellationToken ct = default)
    {
        if (req is null)
        {
            throw new ArgumentNullException(nameof(req));
        }

        if (string.IsNullOrWhiteSpace(req.Text) && string.IsNullOrWhiteSpace(req.ImageDataUrl))
        {
            throw new ArgumentException("At least one input is required: text or image.", nameof(req));
        }

        var files = new List<FilePart>();
        if (!string.IsNullOrWhiteSpace(req.ImageDataUrl))
        {
            files.Add(FromDataUrl(req.ImageDataUrl));
        }

        var input = new VisionNutritionInput(
            Images: files,
            Hint: req.Text,
            UserId: req.UserId ?? "user" // In a real app, get from AuthenticationState
        );

        return await _orchestrator.ProcessVisionNutritionAsync(input, ct);
    }

    private static FilePart FromDataUrl(string dataUrl)
    {
        var m = Regex.Match(dataUrl, @"^data:(?<ct>[^;]+);base64,(?<b64>.+)$",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (!m.Success)
            throw new ArgumentException("Invalid data URL", nameof(dataUrl));

        var contentType = m.Groups["ct"].Value;
        var bytes = Convert.FromBase64String(m.Groups["b64"].Value);
        return new FilePart(bytes, contentType);
    }
}
