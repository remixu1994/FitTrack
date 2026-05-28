namespace FitTrack.Copilot.Api.Contracts;

public record CreateFoodRecordRequest(
    string FoodName,
    double Calories,
    double Protein,
    double Carbs,
    double Fat,
    double? ServingSize,
    string? ServingUnit,
    DateTime? ConsumptionDate,
    string? MealType);

public record FoodRecordDto(
    int Id,
    string FoodName,
    double Calories,
    double Protein,
    double Carbs,
    double Fat,
    double? ServingSize,
    string? ServingUnit,
    DateTime ConsumptionDate,
    string? MealType);

public record AnalyzeFoodRequest(string? Text, string? ImageDataUrl);

public record NutritionSummaryDto(
    double TotalCalories,
    double TotalProtein,
    double TotalCarbs,
    double TotalFat,
    int RecordCount);
