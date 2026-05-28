using System.Text;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public class NutritionTools : INutritionTools
{
    private readonly IUsdaClient _usdaClient;
    private readonly IFoodAiService _foodAiService;
    private readonly IFoodRecordService _foodRecordService;

    public NutritionTools(IUsdaClient usdaClient, IFoodAiService foodAiService, IFoodRecordService foodRecordService)
    {
        _usdaClient = usdaClient;
        _foodAiService = foodAiService;
        _foodRecordService = foodRecordService;
    }

    public async Task<string> SearchUsdaAsync(string query, CancellationToken ct = default)
    {
        var item = await _usdaClient.SearchAsync(query);
        if (item is null)
        {
            return $"No USDA result found for '{query}'.";
        }

        return $"{item.Description} (USDA FDC {item.FdcId}).";
    }

    public async Task<string> AnalyzeMealAsync(string userId, string prompt, CancellationToken ct = default)
    {
        var result = await _foodAiService.AnalyzeAsync(new FoodRequest
        {
            Text = prompt,
            UserId = userId
        }, ct);

        if (result.Items.Count == 0)
        {
            return "No structured food items were detected from the meal description.";
        }

        return $"Estimated {result.TotalCalories:F0} kcal across {result.Items.Count} food item(s).";
    }

    public async Task<AgentNutritionSnapshot> BuildDailyNutritionSnapshotAsync(string userId, string? languageCode, CancellationToken ct = default)
    {
        var summary = await _foodRecordService.GetNutritionSummaryByDateAsync(userId, DateTime.UtcNow.Date, ct);
        return new AgentNutritionSnapshot(
            DayType: summary.TotalCarbs switch
            {
                > 220 => "high",
                > 120 => "medium",
                _ => "low"
            },
            ConsumedCalories: (int)Math.Round(summary.TotalCalories),
            ConsumedProteinG: summary.TotalProtein,
            ConsumedCarbsG: summary.TotalCarbs,
            ConsumedFatG: summary.TotalFat,
            RemainingCalories: Math.Max(0, 2200 - (int)Math.Round(summary.TotalCalories)),
            RemainingProteinG: Math.Max(0, 160 - summary.TotalProtein),
            RemainingCarbsG: Math.Max(0, 220 - summary.TotalCarbs),
            RemainingFatG: Math.Max(0, 70 - summary.TotalFat),
            NextSuggestions: new Dictionary<string, object?>
            {
                ["protein"] = summary.TotalProtein < 120
                    ? AppLanguageSupport.Select(languageCode, "Increase protein in the next meal.", "下一餐提高蛋白质摄入。")
                    : AppLanguageSupport.Select(languageCode, "Protein intake is on track.", "蛋白质摄入基本达标。"),
                ["logging"] = summary.RecordCount == 0
                    ? AppLanguageSupport.Select(languageCode, "Log your first meal to unlock more accurate coaching.", "先记录今天的第一餐，后续建议会更准确。")
                    : AppLanguageSupport.Select(languageCode, "Keep logging meals for tighter estimates.", "继续记录饮食，系统会给出更准确的估算。")
            });
    }
}

public class WorkoutTools : IWorkoutTools
{
    private readonly IFitnessService _fitnessService;
    private readonly IWorkoutSessionService _workoutSessionService;

    public WorkoutTools(IFitnessService fitnessService, IWorkoutSessionService workoutSessionService)
    {
        _fitnessService = fitnessService;
        _workoutSessionService = workoutSessionService;
    }

    public async Task<string> SuggestWorkoutPlanAsync(string userId, string prompt, CancellationToken ct = default)
    {
        var plans = await _fitnessService.GetWorkoutPlansByUserIdAsync(userId, ct);
        if (plans.Count > 0)
        {
            return $"Existing plan: {plans[0].PlanName}. Ask to refine it if you want a new split.";
        }

        return $"Suggested split based on '{prompt}': 3 strength sessions, 2 low-intensity cardio sessions, 2 recovery windows.";
    }

    public async Task<string> SummarizeWorkoutHistoryAsync(string userId, CancellationToken ct = default)
    {
        var sessions = await _workoutSessionService.GetWorkoutSessionsByUserIdAsync(userId, ct);
        if (sessions.Count == 0)
        {
            return "No workout sessions logged yet.";
        }

        var recent = sessions.Take(5).ToList();
        var builder = new StringBuilder();
        builder.AppendLine($"Recent workouts: {recent.Count}");
        foreach (var session in recent)
        {
            builder.AppendLine($"{session.StartTime:yyyy-MM-dd}: {session.Duration ?? 0} min, {session.CaloriesBurned ?? 0:F0} kcal");
        }

        return builder.ToString();
    }
}

public class ProgressTools : IProgressTools
{
    private readonly IProgressService _progressService;

    public ProgressTools(IProgressService progressService)
    {
        _progressService = progressService;
    }

    public Task<ProgressSummaryDto> GetSummaryAsync(string userId, string? languageCode, CancellationToken ct = default)
        => _progressService.GetSummaryAsync(userId, languageCode, ct);
}

public class VisionTools : IVisionTools
{
    private readonly IFoodAiService _foodAiService;

    public VisionTools(IFoodAiService foodAiService)
    {
        _foodAiService = foodAiService;
    }

    public async Task<(string Summary, AgentNutritionSnapshot Snapshot, Dictionary<string, object?> StructuredPayload)> AnalyzeImageAsync(string userId, string? text, string imageDataUrl, string? languageCode, CancellationToken ct = default)
    {
        var result = await _foodAiService.AnalyzeAsync(new FoodRequest
        {
            Text = text,
            ImageDataUrl = imageDataUrl,
            UserId = userId,
            LanguageCode = languageCode
        }, ct);

        var summary = string.IsNullOrWhiteSpace(result.Summary)
            ? AppLanguageSupport.Select(languageCode, $"Detected {result.Items.Count} food item(s), estimated {result.TotalCalories:F0} kcal.", $"识别到 {result.Items.Count} 个食物项目，估算约 {result.TotalCalories:F0} 千卡。")
            : result.Summary;

        var structured = new Dictionary<string, object?>
        {
            ["items"] = result.Items.Select(item => new Dictionary<string, object?>
            {
                ["name"] = item.Name,
                ["calories"] = item.Calories,
                ["proteinGrams"] = item.ProteinGrams,
                ["carbsGrams"] = item.CarbsGrams,
                ["fatGrams"] = item.FatGrams,
                ["confidence"] = item.Confidence,
                ["servingHint"] = item.ServingHint
            }).ToList(),
            ["summary"] = summary
        };

        var snapshot = new AgentNutritionSnapshot(
            ConsumedCalories: (int)Math.Round(result.TotalCalories),
            ConsumedProteinG: result.Items.Sum(i => i.ProteinGrams),
            ConsumedCarbsG: result.Items.Sum(i => i.CarbsGrams),
            ConsumedFatG: result.Items.Sum(i => i.FatGrams),
            NextSuggestions: new Dictionary<string, object?>
            {
                ["vision"] = AppLanguageSupport.Select(languageCode, "Image-based nutrition estimates are approximate.", "基于图片的营养估算仅供参考。"),
                ["follow_up"] = AppLanguageSupport.Select(languageCode, "Confirm portions if you need tighter macro targets.", "如果需要更精确的宏量目标，请补充份量信息。")
            });

        return (summary, snapshot, structured);
    }
}

public class ConversationMemory : IConversationMemory
{
    private readonly IConversationService _conversationService;

    public ConversationMemory(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    public async Task<IReadOnlyList<ConversationMessage>> GetRecentMessagesAsync(string threadId, int count, CancellationToken ct = default)
    {
        var messages = await _conversationService.GetMessagesAsync(threadId, ct);
        return messages.TakeLast(Math.Max(1, count)).ToList();
    }
}
