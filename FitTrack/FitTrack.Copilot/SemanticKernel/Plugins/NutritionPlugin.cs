using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Api.Usda.Models;

namespace FitTrack.Copilot.SemanticKernel.Plugins;

public class NutritionPlugin
{
    private readonly IUsdaClient _usdaClient;

    public NutritionPlugin(IUsdaClient usdaClient)
    {
        _usdaClient = usdaClient;
    }

    /// <summary>
    /// Query nutrition data for a list of food items
    /// </summary>
    public async Task<NutritionResult> GetNutritionAsync(List<string> foodNames, CancellationToken ct = default)
    {
        var result = new NutritionResult();
        
        foreach (var foodName in foodNames)
        {
            try
            {
                var usdaFood = await _usdaClient.SearchAsync(foodName);
               var footd = await _usdaClient.GetFoodAsync(usdaFood.FdcId);
                if (usdaFood != null && footd.FoodNutrients.Any())
                {
                    // Take the first match for simplicity
                    var food = footd.FoodNutrients.First();
                    
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