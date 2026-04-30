using System.Text.Json;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Api;

internal static class Mappers
{
    public static AuthenticatedUserDto ToDto(this ApplicationUser user, UserProfile? profile = null)
        => new(user.Id, user.Email ?? string.Empty, profile?.DisplayName ?? user.Profile?.DisplayName ?? user.UserName ?? user.Email);

    public static UserProfileDto ToDto(this UserProfile profile)
        => new(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.Sex,
            profile.Age,
            profile.HeightCm,
            profile.WeightKg,
            profile.BodyFatPercent,
            profile.ActivityLevel,
            profile.Goal,
            profile.Preferences,
            profile.PreferredAIProvider,
            profile.CreatedAt,
            profile.UpdatedAt);

    public static ConversationThreadDto ToDto(this ConversationThread thread)
        => new(thread.Id, thread.Title, thread.CreatedAt, thread.UpdatedAt, thread.ArchivedAt);

    public static ChatAttachmentDto ToDto(this ConversationAttachment attachment)
        => new(
            attachment.Id,
            attachment.Kind,
            attachment.FileName,
            attachment.MimeType,
            attachment.FileSize,
            $"/api/chat/attachments/{attachment.Id}",
            attachment.CreatedAt);

    public static ChatMessageDto ToDto(this ConversationMessage message)
        => new(
            message.Id,
            message.ThreadId,
            message.Role,
            message.Kind,
            message.ContentText,
            DeserializeJson(message.ContentJson),
            message.Attachments.OrderBy(a => a.CreatedAt).Select(ToDto).ToList(),
            message.TurnIndex,
            message.CreatedAt);

    public static NutritionSnapshotDto ToDto(this NutritionSnapshot snapshot)
        => new(
            snapshot.Id,
            snapshot.ThreadId,
            snapshot.MessageId,
            snapshot.TrainingType,
            snapshot.DayType,
            snapshot.TargetCalories,
            snapshot.TargetProteinG,
            snapshot.TargetCarbsG,
            snapshot.TargetFatG,
            snapshot.ConsumedCalories,
            snapshot.ConsumedProteinG,
            snapshot.ConsumedCarbsG,
            snapshot.ConsumedFatG,
            snapshot.RemainingCalories,
            snapshot.RemainingProteinG,
            snapshot.RemainingCarbsG,
            snapshot.RemainingFatG,
            DeserializeJson(snapshot.NextSuggestions),
            snapshot.CreatedAt);

    public static ThreadDetailDto ToDetailDto(this ConversationThread thread)
        => new(
            thread.Id,
            thread.Title,
            thread.CreatedAt,
            thread.UpdatedAt,
            thread.ArchivedAt,
            thread.Messages.OrderBy(m => m.TurnIndex).Select(ToDto).ToList(),
            thread.Snapshots.OrderByDescending(s => s.CreatedAt).Select(ToDto).ToList());

    private static object? DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }
}
