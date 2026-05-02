using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Api.Usda;
using FoodDetail = FitTrack.Copilot.Api.Usda.Models.FoodDetail;

namespace FitTrack.Copilot.AI.Plugins;

public class NutritionPlugin
{
    private readonly IUsdaClient _usdaClient;

    public NutritionPlugin(IUsdaClient usdaClient)
    {
        _usdaClient = usdaClient;
    }

    /// <summary>
    /// Query nutrition data for a list of food items.
    /// </summary>
    public async Task<NutritionResult> GetNutritionAsync(List<string> foodNames, CancellationToken ct = default)
    {
        var result = new NutritionResult();

        foreach (var foodName in foodNames)
        {
            try
            {
                var usdaFood = await _usdaClient.SearchAsync(foodName);
                if (usdaFood is null)
                {
                    result.Items.Add(CreateUnknownItem(foodName));
                    continue;
                }

                var foodDetail = await _usdaClient.GetFoodAsync(usdaFood.FdcId);
                if (foodDetail is null)
                {
                    result.Items.Add(CreateUnknownItem(foodName));
                    continue;
                }

                result.Items.Add(new NutritionItem
                {
                    Name = usdaFood.Description ?? foodName,
                    Calories = GetNutrientAmount(foodDetail, 1008), // Energy (kcal)
                    ProteinGrams = GetNutrientAmount(foodDetail, 1003), // Protein
                    CarbsGrams = GetNutrientAmount(foodDetail, 1005), // Carbohydrate
                    FatGrams = GetNutrientAmount(foodDetail, 1004), // Total lipid (fat)
                    ServingHint = "per USDA entry",
                    Source = "usda"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get nutrition data for {foodName}: {ex.Message}");
                result.Items.Add(CreateUnknownItem(foodName, "error"));
            }
        }

        return result;
    }

    private static double GetNutrientAmount(FoodDetail detail, int nutrientId)
    {
        var nutrient = detail.FoodNutrients.FirstOrDefault(n => n.NutrientId == nutrientId);
        return nutrient?.Amount ?? 0;
    }

    private static NutritionItem CreateUnknownItem(string foodName, string source = "unknown")
    {
        return new NutritionItem
        {
            Name = foodName,
            Calories = 0,
            ProteinGrams = 0,
            CarbsGrams = 0,
            FatGrams = 0,
            Source = source
        };
    }
}
