using System.Text.Json;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Api;

internal static class Mappers
{
    public static AuthenticatedUserDto ToDto(this ApplicationUser user, IEnumerable<string> roles, UserProfile? profile = null)
        => new(user.Id, user.Email ?? string.Empty, profile?.DisplayName ?? user.Profile?.DisplayName ?? user.UserName ?? user.Email, roles.ToArray());

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
            profile.PreferredModelConnectorId,
            profile.CreatedAt,
            profile.UpdatedAt);

    public static TenantSummaryDto ToDto(this Tenant tenant)
        => new(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsSystemDefault,
            tenant.Users.Count,
            tenant.ModelConnectors.Count);

    public static TenantModelConnectorPresetDto ToDto(this TenantModelConnectorPreset preset)
        => new(
            preset.Key,
            preset.DisplayName,
            preset.Protocol.ToString(),
            preset.BaseUrl,
            preset.ModelId);

    public static TenantModelConnectorAdminDto ToAdminDto(this TenantModelConnector connector)
        => new(
            connector.Id,
            connector.TenantId,
            connector.DisplayName,
            connector.ProviderPreset,
            connector.Protocol.ToString(),
            connector.BaseUrl,
            connector.ModelId,
            connector.IsDefault,
            connector.IsEnabled,
            !string.IsNullOrWhiteSpace(connector.EncryptedApiKey),
            connector.InputTokenPricePer1M,
            connector.OutputTokenPricePer1M,
            connector.CacheReadTokenPricePer1M,
            connector.CacheWriteTokenPricePer1M,
            connector.CreatedAt,
            connector.UpdatedAt);

    public static TenantModelConnectorOptionDto ToOptionDto(this TenantModelConnector connector)
        => new(
            connector.Id,
            connector.DisplayName,
            connector.ProviderPreset,
            connector.Protocol.ToString(),
            connector.ModelId,
            connector.IsDefault);

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
