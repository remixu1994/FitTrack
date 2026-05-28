using FitTrack.Copilot.Service;
namespace FitTrack.Copilot.Agents;

public class ProgressCheckInAgent
{
    private readonly IProgressTools _progressTools;

    public ProgressCheckInAgent(IProgressTools progressTools)
    {
        _progressTools = progressTools;
    }

    public async Task<AgentExecutionResult> RunAsync(string userId, string? languageCode, CancellationToken ct = default)
    {
        var summary = await _progressTools.GetSummaryAsync(userId, languageCode, ct);
        var structuredPayload = new Dictionary<string, object?>
        {
            ["headline"] = summary.Headline,
            ["recommendations"] = summary.Recommendations,
            ["currentWeightKg"] = summary.CurrentWeightKg,
            ["bodyFatPercent"] = summary.BodyFatPercent
        };

        var snapshot = new AgentNutritionSnapshot(
            ConsumedCalories: (int)Math.Round(summary.CaloriesToday),
            ConsumedProteinG: summary.ProteinToday,
            ConsumedCarbsG: summary.CarbsToday,
            ConsumedFatG: summary.FatToday,
            NextSuggestions: summary.Recommendations
                .Select((value, index) => new { value, index })
                .ToDictionary(x => $"item_{x.index + 1}", x => (object?)x.value));

        return new AgentExecutionResult("ProgressCheckInAgent", summary.Headline, structuredPayload, snapshot, new[] { "subagent:progress", "progress.summary" });
    }
}
