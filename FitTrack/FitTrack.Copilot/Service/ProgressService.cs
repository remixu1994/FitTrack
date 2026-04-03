using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class ProgressService : IProgressService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProfileService _profileService;
    private readonly IFoodRecordService _foodRecordService;
    private readonly IWorkoutSessionService _workoutSessionService;

    public ProgressService(
        ApplicationDbContext dbContext,
        IProfileService profileService,
        IFoodRecordService foodRecordService,
        IWorkoutSessionService workoutSessionService)
    {
        _dbContext = dbContext;
        _profileService = profileService;
        _foodRecordService = foodRecordService;
        _workoutSessionService = workoutSessionService;
    }

    public async Task<ProgressSummaryDto> GetSummaryAsync(string userId, CancellationToken ct = default)
    {
        var profile = await _profileService.GetOrCreateProfileAsync(userId, ct: ct);
        var today = DateTime.UtcNow.Date;
        var todaySummary = await _foodRecordService.GetNutritionSummaryByDateAsync(userId, today, ct);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var workouts = await _workoutSessionService.GetWorkoutSessionsByDateRangeAsync(userId, weekStart, today.AddDays(1), ct);

        var headline = todaySummary.RecordCount == 0
            ? "No food log yet today. Start with your first meal or ask the coach for a plan."
            : $"You've logged {todaySummary.TotalCalories:F0} kcal today across {todaySummary.RecordCount} meal entries.";

        var recommendations = new List<string>();
        if (todaySummary.TotalProtein < 100)
        {
            recommendations.Add("Protein intake looks light. Consider a high-protein meal or snack.");
        }
        if (workouts.Count == 0)
        {
            recommendations.Add("No workout logged this week yet. Schedule a short session to maintain consistency.");
        }
        if (recommendations.Count == 0)
        {
            recommendations.Add("Momentum looks stable. Keep logging meals and workouts for better coaching.");
        }

        return new ProgressSummaryDto(
            userId,
            profile.WeightKg,
            profile.BodyFatPercent,
            todaySummary.RecordCount,
            workouts.Count,
            todaySummary.TotalCalories,
            todaySummary.TotalProtein,
            todaySummary.TotalCarbs,
            todaySummary.TotalFat,
            headline,
            recommendations);
    }
}
