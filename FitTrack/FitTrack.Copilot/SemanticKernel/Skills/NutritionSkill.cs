using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Api.Usda;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FitTrack.Copilot.SemanticKernel.Skills;

public class NutritionSkill
{
    private readonly IUsdaClient _usdaClient;

    public NutritionSkill(IUsdaClient usdaClient)
    {
        _usdaClient = usdaClient;
    }

    [KernelFunction, Description("Query nutrition data for a list of food items")]
    public async Task<NutritionResult> GetNutritionAsync(
        [Description("List of food item names")] List<string> foodNames,
        CancellationToken ct = default)
    {
        var result = new NutritionResult();
        
        foreach (var foodName in foodNames)
        {
            try
            {
                var usdaFood = await _usdaClient.SearchAsync(foodName);
                var foodDetails = await _usdaClient.GetFoodAsync(usdaFood.FdcId);
                if (usdaFood != null && foodDetails.FoodNutrients.Any())
                {
                    // Take the first match for simplicity
                    var food = foodDetails.FoodNutrients.First();
                    
                    var nutritionItem = new NutritionItem
                    {
                        Name = food.Name,
                        Calories = food.Amount,
                        ServingHint = food.UnitName,
                        Source = "usda"
                    };
                    
                    result.Items.Add(nutritionItem);
                }
                else
                {
                    // Fallback: create a placeholder item if no USDA data found
                    result.Items.Add(new NutritionItem
                    {
                        Name = foodName,
                        Calories = 0,
                        ProteinGrams = 0,
                        CarbsGrams = 0,
                        FatGrams = 0,
                        Source = "unknown"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing other items
                Console.WriteLine($"Failed to get nutrition data for {foodName}: {ex.Message}");
                
                // Add a placeholder item
                result.Items.Add(new NutritionItem
                {
                    Name = foodName,
                    Calories = 0,
                    ProteinGrams = 0,
                    CarbsGrams = 0,
                    FatGrams = 0,
                    Source = "error"
                });
            }
        }
        
        return result;
    }
}
