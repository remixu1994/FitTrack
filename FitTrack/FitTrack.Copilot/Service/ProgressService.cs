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

    public async Task<ProgressSummaryDto> GetSummaryAsync(string userId, string? languageCode = null, CancellationToken ct = default)
    {
        var profile = await _profileService.GetOrCreateProfileAsync(userId, ct: ct);
        var today = DateTime.UtcNow.Date;
        var todaySummary = await _foodRecordService.GetNutritionSummaryByDateAsync(userId, today, ct);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var workouts = await _workoutSessionService.GetWorkoutSessionsByDateRangeAsync(userId, weekStart, today.AddDays(1), ct);

        var headline = todaySummary.RecordCount == 0
            ? AppLanguageSupport.Select(languageCode,
                "No food log yet today. Start with your first meal or ask the coach for a plan.",
                "今天还没有饮食记录。先记录第一餐，或直接让教练帮你规划。")
            : AppLanguageSupport.Select(languageCode,
                $"You've logged {todaySummary.TotalCalories:F0} kcal today across {todaySummary.RecordCount} meal entries.",
                $"今天已记录 {todaySummary.RecordCount} 条饮食，累计约 {todaySummary.TotalCalories:F0} 千卡。");

        var recommendations = new List<string>();
        if (todaySummary.TotalProtein < 100)
        {
            recommendations.Add(AppLanguageSupport.Select(languageCode,
                "Protein intake looks light. Consider a high-protein meal or snack.",
                "当前蛋白质摄入偏低，建议补一顿高蛋白正餐或加餐。"));
        }
        if (workouts.Count == 0)
        {
            recommendations.Add(AppLanguageSupport.Select(languageCode,
                "No workout logged this week yet. Schedule a short session to maintain consistency.",
                "本周还没有训练记录，建议先安排一次短训练保持节奏。"));
        }
        if (recommendations.Count == 0)
        {
            recommendations.Add(AppLanguageSupport.Select(languageCode,
                "Momentum looks stable. Keep logging meals and workouts for better coaching.",
                "当前节奏比较稳定，继续记录饮食和训练可以获得更好的建议。"));
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
