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

    public async Task<AgentNutritionSnapshot> BuildDailyNutritionSnapshotAsync(string userId, CancellationToken ct = default)
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
                ["protein"] = summary.TotalProtein < 120 ? "Increase protein in the next meal." : "Protein intake is on track.",
                ["logging"] = summary.RecordCount == 0 ? "Log your first meal to unlock more accurate coaching." : "Keep logging meals for tighter estimates."
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

    public Task<ProgressSummaryDto> GetSummaryAsync(string userId, CancellationToken ct = default)
        => _progressService.GetSummaryAsync(userId, ct);
}

public class VisionTools : IVisionTools
{
    private readonly IFoodAiService _foodAiService;

    public VisionTools(IFoodAiService foodAiService)
    {
        _foodAiService = foodAiService;
    }

    public async Task<(string Summary, AgentNutritionSnapshot Snapshot, Dictionary<string, object?> StructuredPayload)> AnalyzeImageAsync(string userId, string? text, string imageDataUrl, CancellationToken ct = default)
    {
        var result = await _foodAiService.AnalyzeAsync(new FoodRequest
        {
            Text = text,
            ImageDataUrl = imageDataUrl,
            UserId = userId
        }, ct);

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
            ["summary"] = result.Summary
        };

        var snapshot = new AgentNutritionSnapshot(
            ConsumedCalories: (int)Math.Round(result.TotalCalories),
            ConsumedProteinG: result.Items.Sum(i => i.ProteinGrams),
            ConsumedCarbsG: result.Items.Sum(i => i.CarbsGrams),
            ConsumedFatG: result.Items.Sum(i => i.FatGrams),
            NextSuggestions: new Dictionary<string, object?>
            {
                ["vision"] = "Image-based nutrition estimates are approximate.",
                ["follow_up"] = "Confirm portions if you need tighter macro targets."
            });

        var summary = string.IsNullOrWhiteSpace(result.Summary)
            ? $"Detected {result.Items.Count} food item(s), estimated {result.TotalCalories:F0} kcal."
            : result.Summary;

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
