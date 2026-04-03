namespace FitTrack.Copilot.Api.Contracts;

public record ConversationThreadDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ArchivedAt);

public record ChatAttachmentDto(
    string Id,
    string Kind,
    string? FileName,
    string? MimeType,
    long? FileSize,
    string DownloadPath,
    DateTime CreatedAt);

public record ChatMessageDto(
    string Id,
    string ThreadId,
    string Role,
    string Kind,
    string? ContentText,
    object? ContentJson,
    IReadOnlyList<ChatAttachmentDto> Attachments,
    int TurnIndex,
    DateTime CreatedAt);

public record NutritionSnapshotDto(
    string Id,
    string ThreadId,
    string MessageId,
    string? TrainingType,
    string? DayType,
    int? TargetCalories,
    int? TargetProteinG,
    int? TargetCarbsG,
    int? TargetFatG,
    int? ConsumedCalories,
    double? ConsumedProteinG,
    double? ConsumedCarbsG,
    double? ConsumedFatG,
    int? RemainingCalories,
    double? RemainingProteinG,
    double? RemainingCarbsG,
    double? RemainingFatG,
    object? NextSuggestions,
    DateTime CreatedAt);

public record ThreadDetailDto(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ArchivedAt,
    IReadOnlyList<ChatMessageDto> Messages,
    IReadOnlyList<NutritionSnapshotDto> Snapshots);

public record CreateThreadRequest(string Title);

public record MealPhotoAttachmentDto(string DataUrl, string? Name, string? MimeType, long? Size);

public record SendMessageRequest(
    string ThreadId,
    string? ContentText,
    Dictionary<string, object?>? ContentJson,
    MealPhotoAttachmentDto? MealPhoto);

public record AgentResponseEnvelope(
    string AgentName,
    string Message,
    Dictionary<string, object?>? StructuredPayload = null,
    NutritionSnapshotDto? Snapshot = null,
    IReadOnlyList<string>? ToolEvents = null);
