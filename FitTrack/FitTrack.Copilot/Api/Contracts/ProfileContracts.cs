namespace FitTrack.Copilot.Api.Contracts;

public record UserProfileDto(
    string Id,
    string UserId,
    string? DisplayName,
    string? Sex,
    int? Age,
    double? HeightCm,
    double? WeightKg,
    double? BodyFatPercent,
    string? ActivityLevel,
    string? Goal,
    string? Preferences,
    string? PreferredModelConnectorId,
    string? EffectiveModelConnectorId,
    string? EffectiveModelConnectorDisplayName,
    string? EffectiveModelConnectorModelId,
    string? EffectiveModelConnectorProviderPreset,
    bool EffectiveModelConnectorIsTenantDefault,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record UpsertUserProfileRequest(
    string? DisplayName,
    string? Sex,
    int? Age,
    double? HeightCm,
    double? WeightKg,
    double? BodyFatPercent,
    string? ActivityLevel,
    string? Goal,
    string? Preferences,
    string? PreferredModelConnectorId);
