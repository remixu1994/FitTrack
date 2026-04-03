using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface INutritionTools
{
    Task<string> SearchUsdaAsync(string query, CancellationToken ct = default);
    Task<string> AnalyzeMealAsync(string userId, string prompt, CancellationToken ct = default);
    Task<AgentNutritionSnapshot> BuildDailyNutritionSnapshotAsync(string userId, CancellationToken ct = default);
}

public interface IWorkoutTools
{
    Task<string> SuggestWorkoutPlanAsync(string userId, string prompt, CancellationToken ct = default);
    Task<string> SummarizeWorkoutHistoryAsync(string userId, CancellationToken ct = default);
}

public interface IProgressTools
{
    Task<ProgressSummaryDto> GetSummaryAsync(string userId, CancellationToken ct = default);
}

public interface IVisionTools
{
    Task<(string Summary, AgentNutritionSnapshot Snapshot, Dictionary<string, object?> StructuredPayload)> AnalyzeImageAsync(string userId, string? text, string imageDataUrl, CancellationToken ct = default);
}

public interface IConversationMemory
{
    Task<IReadOnlyList<ConversationMessage>> GetRecentMessagesAsync(string threadId, int count, CancellationToken ct = default);
}
