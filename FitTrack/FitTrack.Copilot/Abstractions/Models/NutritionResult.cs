namespace FitTrack.Copilot.Abstractions.Models;

/// <summary>
/// Structured nutrition analysis result returned by AI plugins (e.g., Vision/Text).
/// </summary>
public sealed class NutritionResult
{
    /// <summary>
    /// List of detected or inferred food items.
    /// </summary>
    public List<NutritionItem> Items { get; set; } = new();

    /// <summary>
    /// Convenience property: sum of all item calories.
    /// </summary>
    public double TotalCalories => Items.Sum(i => i.Calories);

    /// <summary>
    /// Optional note or summary text produced by the model.
    /// </summary>
    public string? Summary { get; set; }

    public override string ToString() => $"{Items.Count} items, {TotalCalories:F0} kcal";
}

/// <summary>
/// A single food component with macro nutrients.
/// </summary>
public sealed class NutritionItem
{
    /// <summary>Human-readable name, e.g. "beef noodles", "fried egg".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Total calories (kcal) for the given portion.</summary>
    public double Calories { get; set; }

    /// <summary>Protein in grams.</summary>
    public double ProteinGrams { get; set; }

    /// <summary>Carbohydrates in grams.</summary>
    public double CarbsGrams { get; set; }

    /// <summary>Fat in grams.</summary>
    public double FatGrams { get; set; }

    /// <summary>Optional model confidence 0~1.</summary>
    public double? Confidence { get; set; }

    /// <summary>Optional serving hint such as "half bowl" or "1 cup".</summary>
    public string? ServingHint { get; set; }

    /// <summary>Indicates data source: "ai" or "manual".</summary>
    public string Source { get; set; } = "ai";
}