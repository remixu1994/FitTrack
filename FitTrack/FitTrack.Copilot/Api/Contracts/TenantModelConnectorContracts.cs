namespace FitTrack.Copilot.Api.Contracts;

public record TenantSummaryDto(
    string Id,
    string Name,
    string Slug,
    bool IsSystemDefault,
    int UserCount,
    int ConnectorCount);

public record TenantModelConnectorPresetDto(
    string Key,
    string DisplayName,
    string Protocol,
    string BaseUrl,
    string ModelId);

public record TenantModelConnectorAdminDto(
    string Id,
    string TenantId,
    string DisplayName,
    string ProviderPreset,
    string Protocol,
    string BaseUrl,
    string ModelId,
    bool IsDefault,
    bool IsEnabled,
    bool HasApiKey,
    double? InputTokenPricePer1M,
    double? OutputTokenPricePer1M,
    double? CacheReadTokenPricePer1M,
    double? CacheWriteTokenPricePer1M,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TenantModelConnectorOptionDto(
    string Id,
    string DisplayName,
    string ProviderPreset,
    string Protocol,
    string ModelId,
    bool IsDefault);

public record UpsertTenantModelConnectorRequest(
    string DisplayName,
    string ProviderPreset,
    string Protocol,
    string BaseUrl,
    string ModelId,
    string? ApiKey,
    double? InputTokenPricePer1M,
    double? OutputTokenPricePer1M,
    double? CacheReadTokenPricePer1M,
    double? CacheWriteTokenPricePer1M,
    bool IsDefault,
    bool IsEnabled);
