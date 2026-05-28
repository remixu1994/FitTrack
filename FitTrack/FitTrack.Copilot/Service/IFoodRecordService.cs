using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IFoodRecordService
{
    Task<FoodRecord> CreateFoodRecordAsync(FoodRecord record, CancellationToken ct = default);
    Task<FoodRecord?> GetFoodRecordAsync(int id, CancellationToken ct = default);
    Task<List<FoodRecord>> GetFoodRecordsByUserIdAsync(string userId, CancellationToken ct = default);
    Task<FoodRecord> UpdateFoodRecordAsync(FoodRecord record, CancellationToken ct = default);
    Task DeleteFoodRecordAsync(int id, CancellationToken ct = default);
    Task<List<FoodRecord>> GetFoodRecordsByDateAsync(string userId, DateTime date, CancellationToken ct = default);
    Task<NutritionSummary> GetNutritionSummaryByDateAsync(string userId, DateTime date, CancellationToken ct = default);
}

public class NutritionSummary
{
    public double TotalCalories { get; set; }
    public double TotalProtein { get; set; }
    public double TotalCarbs { get; set; }
    public double TotalFat { get; set; }
    public int RecordCount { get; set; }
}
