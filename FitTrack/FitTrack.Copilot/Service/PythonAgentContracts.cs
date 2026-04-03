using System.Text.Json.Serialization;

namespace FitTrack.Copilot.Service;

public sealed record PythonRecentMessage(
    string Role,
    string Kind,
    string? ContentText,
    object? ContentJson);

public sealed record PythonChatRequest(
    string UserId,
    string ThreadId,
    string? Text,
    string? MealPhotoDataUrl,
    IReadOnlyList<PythonRecentMessage> RecentMessages,
    DateTime RequestTimestampUtc);

public sealed record PythonChatResponse(
    string AgentName,
    string Message,
    Dictionary<string, object?>? StructuredPayload,
    AgentNutritionSnapshot? Snapshot,
    IReadOnlyList<string>? ToolEvents,
    string? TraceId);
