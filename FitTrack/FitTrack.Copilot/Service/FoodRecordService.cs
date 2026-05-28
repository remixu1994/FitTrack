using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class FoodRecordService : IFoodRecordService
{
    private readonly ApplicationDbContext _dbContext;

    public FoodRecordService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FoodRecord> CreateFoodRecordAsync(FoodRecord record, CancellationToken ct = default)
    {
        _dbContext.FoodRecords.Add(record);
        await _dbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task<FoodRecord?> GetFoodRecordAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.FoodRecords.FindAsync(id, ct);
    }

    public async Task<List<FoodRecord>> GetFoodRecordsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.FoodRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ConsumptionDate)
            .ToListAsync(ct);
    }

    public async Task<FoodRecord> UpdateFoodRecordAsync(FoodRecord record, CancellationToken ct = default)
    {
        _dbContext.FoodRecords.Update(record);
        await _dbContext.SaveChangesAsync(ct);
        return record;
    }

    public async Task DeleteFoodRecordAsync(int id, CancellationToken ct = default)
    {
        var record = await _dbContext.FoodRecords.FindAsync(id, ct);
        if (record != null)
        {
            _dbContext.FoodRecords.Remove(record);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<List<FoodRecord>> GetFoodRecordsByDateAsync(string userId, DateTime date, CancellationToken ct = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        return await _dbContext.FoodRecords
            .Where(r => r.UserId == userId && r.ConsumptionDate >= startOfDay && r.ConsumptionDate <= endOfDay)
            .OrderBy(r => r.ConsumptionDate)
            .ToListAsync(ct);
    }

    public async Task<NutritionSummary> GetNutritionSummaryByDateAsync(string userId, DateTime date, CancellationToken ct = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

        var records = await _dbContext.FoodRecords
            .Where(r => r.UserId == userId && r.ConsumptionDate >= startOfDay && r.ConsumptionDate <= endOfDay)
            .ToListAsync(ct);

        var summary = new NutritionSummary
        {
            TotalCalories = records.Sum(r => r.Calories),
            TotalProtein = records.Sum(r => r.Protein),
            TotalCarbs = records.Sum(r => r.Carbs),
            TotalFat = records.Sum(r => r.Fat),
            RecordCount = records.Count
        };

        return summary;
    }
}
