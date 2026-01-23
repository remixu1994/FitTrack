using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.SemanticKernel.Plugins;

namespace FitTrack.Copilot.SemanticKernel.Orchestrator;

public class FoodNutritionOrchestrator
{
    private readonly VisionFoodRecognitionPlugin _visionPlugin;
    private readonly NutritionPlugin _nutritionPlugin;

    public FoodNutritionOrchestrator(
        VisionFoodRecognitionPlugin visionPlugin,
        NutritionPlugin nutritionPlugin)
    {
        _visionPlugin = visionPlugin;
        _nutritionPlugin = nutritionPlugin;
    }

    /// <summary>
    /// Process vision nutrition request by coordinating vision recognition and nutrition lookup
    /// </summary>
    public async Task<NutritionResult> ProcessVisionNutritionAsync(VisionNutritionInput input, CancellationToken ct = default)
    {
        // Step 1: Use VisionFoodRecognitionPlugin to identify food items from images
        var recognizedFoodItems = await _visionPlugin.RecognizeFoodFromImagesAsync(input, ct);
        
        if (!recognizedFoodItems.Any())
        {
            return new NutritionResult 
            { 
                Summary = "No food items confidently detected in the image."
            };
        }
        
        // Extract food names for nutrition lookup
        var foodNames = recognizedFoodItems.Select(item => item.Name).ToList();
        
        // Step 2: Use NutritionPlugin to get detailed nutrition data
        var nutritionResult = await _nutritionPlugin.GetNutritionAsync(foodNames, ct);
        
        // Step 3: Enhance the nutrition result with additional metadata from recognition
        for (int i = 0; i < nutritionResult.Items.Count && i < recognizedFoodItems.Count; i++)
        {
            var recognitionItem = recognizedFoodItems[i];
            var nutritionItem = nutritionResult.Items[i];
            
            // Apply serving hints from vision recognition if not already present
            if (string.IsNullOrEmpty(nutritionItem.ServingHint) && !string.IsNullOrEmpty(recognitionItem.ServingHint))
            {
                nutritionItem.ServingHint = recognitionItem.ServingHint;
            }
            
            // Apply confidence score if available
            if (recognitionItem.Confidence.HasValue)
            {
                nutritionItem.Confidence = recognitionItem.Confidence;
            }
        }
        
        // Generate summary
        nutritionResult.Summary = $"Detected {recognizedFoodItems.Count} food items totaling {nutritionResult.TotalCalories:F0} kcal.";
        
        return nutritionResult;
    }
}