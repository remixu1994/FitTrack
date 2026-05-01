namespace FitTrack.Copilot.Service;

public record AgentNutritionSnapshot(
    string? TrainingType = null,
    string? DayType = null,
    int? TargetCalories = null,
    int? TargetProteinG = null,
    int? TargetCarbsG = null,
    int? TargetFatG = null,
    int? ConsumedCalories = null,
    double? ConsumedProteinG = null,
    double? ConsumedCarbsG = null,
    double? ConsumedFatG = null,
    int? RemainingCalories = null,
    double? RemainingProteinG = null,
    double? RemainingCarbsG = null,
    double? RemainingFatG = null,
    Dictionary<string, object?>? NextSuggestions = null);

public record AgentExecutionResult(
    string AgentName,
    string Message,
    Dictionary<string, object?>? StructuredPayload = null,
    AgentNutritionSnapshot? Snapshot = null,
    IReadOnlyList<string>? ToolEvents = null);

public enum CoachStreamEventType
{
    Token,
    ToolEvent,
    Completed
}

public record CoachStreamEvent(
    CoachStreamEventType Type,
    string? Value = null,
    AgentExecutionResult? Result = null)
{
    public static CoachStreamEvent Token(string value) => new(CoachStreamEventType.Token, value);

    public static CoachStreamEvent ToolEvent(string value) => new(CoachStreamEventType.ToolEvent, value);

    public static CoachStreamEvent Completed(AgentExecutionResult result) => new(CoachStreamEventType.Completed, Result: result);
}
