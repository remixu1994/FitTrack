using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.AI.Plugins;

namespace FitTrack.Copilot.AI.Orchestrator;

public class FoodNutritionOrchestrator
{
    private readonly VisionFoodRecognitionPlugin _visionPlugin;
    private readonly TextFoodRecognitionPlugin _textPlugin;
    private readonly NutritionPlugin _nutritionPlugin;

    public FoodNutritionOrchestrator(
        VisionFoodRecognitionPlugin visionPlugin,
        TextFoodRecognitionPlugin textPlugin,
        NutritionPlugin nutritionPlugin)
    {
        _visionPlugin = visionPlugin;
        _textPlugin = textPlugin;
        _nutritionPlugin = nutritionPlugin;
    }

    /// <summary>
    /// Process nutrition request from text/image/mixed input.
    /// </summary>
    public async Task<NutritionResult> ProcessVisionNutritionAsync(
        VisionNutritionInput input,
        CancellationToken ct = default)
    {
        var recognizedFoodItems = new List<FoodItem>();

        if (input.Images is { Count: > 0 })
        {
            var visionItems = await _visionPlugin.RecognizeFoodFromImagesAsync(input, ct);
            recognizedFoodItems.AddRange(visionItems);
        }

        if (!string.IsNullOrWhiteSpace(input.Hint))
        {
            var textItems = await _textPlugin.RecognizeFoodFromTextAsync(input.Hint, ct);
            recognizedFoodItems.AddRange(textItems);
        }

        recognizedFoodItems = recognizedFoodItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim().ToLowerInvariant())
            .Select(g => g.OrderByDescending(i => i.Confidence ?? 0).First())
            .ToList();

        if (!recognizedFoodItems.Any())
        {
            return new NutritionResult
            {
                Summary = "No food items detected from text/image input."
            };
        }

        var foodNames = recognizedFoodItems.Select(item => item.Name).ToList();
        var nutritionResult = await _nutritionPlugin.GetNutritionAsync(foodNames, ct);

        for (int i = 0; i < nutritionResult.Items.Count && i < recognizedFoodItems.Count; i++)
        {
            var recognitionItem = recognizedFoodItems[i];
            var nutritionItem = nutritionResult.Items[i];

            if (string.IsNullOrEmpty(nutritionItem.ServingHint) && !string.IsNullOrEmpty(recognitionItem.ServingHint))
            {
                nutritionItem.ServingHint = recognitionItem.ServingHint;
            }

            if (recognitionItem.Confidence.HasValue)
            {
                nutritionItem.Confidence = recognitionItem.Confidence;
            }
        }

        nutritionResult.Summary =
            $"Detected {recognizedFoodItems.Count} food items totaling {nutritionResult.TotalCalories:F0} kcal.";

        return nutritionResult;
    }
}
