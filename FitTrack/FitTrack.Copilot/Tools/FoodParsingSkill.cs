using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Api.Usda;
using Microsoft.SemanticKernel;

namespace FitTrack.Copilot.Tools;

public class FoodLookupSkill(IUsdaClient client)
{
    [KernelFunction]
    public async Task<double?> LookupCalories(string foodName)
    {
        var item =  await client.SearchAsync(foodName);
        if (item == null) 
            return null;

        var detail = await client.GetFoodAsync(item.FdcId);

        var energy = detail?.FoodNutrients
            .FirstOrDefault(n => n.Name.Contains("Energy", StringComparison.OrdinalIgnoreCase));

        return energy?.Amount;
    }
}

