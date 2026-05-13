using FitTrack.Copilot.Abstractions.Models;

namespace FitTrack.Copilot.Service;

public interface IFoodAiService
{
    Task<NutritionResult> AnalyzeAsync(FoodRequest req, CancellationToken ct = default);
}

public class FoodRequest
{
    public string? Text { get; set; }
    public string? ImageDataUrl { get; set; }
    public string? UserId { get; set; }
    public string? LanguageCode { get; set; }
    public string? ServiceId { get; set; }
    public string? ModelId { get; set; }
}

public class FoodResponse
{
    public string Brief { get; set; } = "Here is the estimate:";
    public NutritionResult Nutrition { get; set; } = new();
}
