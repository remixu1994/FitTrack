namespace FitTrack.Copilot.Models;

/// <summary>
/// Represents a recognized food item from image analysis
/// </summary>
public sealed class FoodItem
{
    /// <summary>
    /// Human-readable name of the food item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estimated portion size or serving hint
    /// </summary>
    public string? ServingHint { get; set; }

    /// <summary>
    /// Optional model confidence score (0-1)
    /// </summary>
    public double? Confidence { get; set; }

    /// <summary>
    /// Nutritional information for this food item
    /// </summary>
    public NutritionInfo? Nutrition { get; set; }
}

/// <summary>
/// Detailed nutritional information for a food item
/// </summary>
public sealed class NutritionInfo
{
    /// <summary>Total calories (kcal) for the given portion.</summary>
    public double Calories { get; set; }

    /// <summary>Protein in grams.</summary>
    public double ProteinGrams { get; set; }

    /// <summary>Carbohydrates in grams.</summary>
    public double CarbsGrams { get; set; }

    /// <summary>Fat in grams.</summary>
    public double FatGrams { get; set; }
}