namespace FitTrack.Copilot.Api.Contracts;

public record ProgressSummaryDto(
    string UserId,
    double? CurrentWeightKg,
    double? BodyFatPercent,
    int FoodRecordCountToday,
    int WorkoutCountThisWeek,
    double CaloriesToday,
    double ProteinToday,
    double CarbsToday,
    double FatToday,
    string Headline,
    IReadOnlyList<string> Recommendations);
