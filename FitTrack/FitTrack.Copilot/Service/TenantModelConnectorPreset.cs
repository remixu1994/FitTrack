using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public sealed record TenantModelConnectorPreset(
    string Key,
    string DisplayName,
    TenantModelProtocol Protocol,
    string BaseUrl,
    string ModelId,
    string? LegacyProvider = null,
    string? ConfigurationSection = null,
    string? SeedConnectorId = null);
