using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FitTrack.Copilot.MAF.Skills;

public class NutritionSkill
{
    [KernelFunction, Description("Provides nutrition information for common foods")]
    public async Task<string> GetFoodNutritionAsync(
        [Description("Name of the food")] string foodName,
        CancellationToken cancellationToken = default)
    {
        // 这里可以集成 USDA API 或其他营养数据库
        // 暂时返回模拟数据
        return $"Nutrition information for {foodName}:\n" +
               "- Calories: 100\n" +
               "- Protein: 5g\n" +
               "- Carbohydrates: 15g\n" +
               "- Fat: 3g";
    }

    [KernelFunction, Description("Suggests meal plans based on dietary preferences")]
    public async Task<string> SuggestMealPlanAsync(
        [Description("Dietary preference (e.g., vegetarian, keto, paleo)")] string dietType,
        CancellationToken cancellationToken = default)
    {
        return $"Meal plan suggestion for {dietType} diet:\n" +
               "- Breakfast: Oatmeal with fruits\n" +
               "- Lunch: Grilled chicken salad\n" +
               "- Dinner: Baked salmon with vegetables";
    }
}
