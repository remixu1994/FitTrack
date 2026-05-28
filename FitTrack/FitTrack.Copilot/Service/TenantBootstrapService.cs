using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public interface ITenantBootstrapService
{
    Task EnsureSystemTenantAsync(CancellationToken ct = default);
}

public sealed class TenantBootstrapService : ITenantBootstrapService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IConnectorSecretProtector _secretProtector;

    public TenantBootstrapService(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        IConnectorSecretProtector secretProtector)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _secretProtector = secretProtector;
    }

    public async Task EnsureSystemTenantAsync(CancellationToken ct = default)
    {
        var seedOptions = ModelConnectorSeedOptions.FromConfiguration(_configuration);
        var tenant = await _dbContext.Tenants
            .Include(item => item.ModelConnectors)
            .FirstOrDefaultAsync(item => item.Id == TenantConstants.DefaultTenantId, ct);

        if (tenant is null)
        {
            tenant = new Tenant
            {
                Id = TenantConstants.DefaultTenantId,
                Name = TenantConstants.DefaultTenantName,
                Slug = TenantConstants.DefaultTenantSlug,
                IsSystemDefault = true
            };
            _dbContext.Tenants.Add(tenant);
        }

        foreach (var preset in TenantModelConnectorPresetCatalog.All)
        {
            var isConfiguredDefault = seedOptions.IsDefaultPreset(preset);
            var connector = tenant.ModelConnectors.FirstOrDefault(item => item.ProviderPreset == preset.Key);
            if (connector is null)
            {
                connector = new TenantModelConnector
                {
                    Id = preset.SeedConnectorId ?? Guid.NewGuid().ToString("N"),
                    TenantId = tenant.Id,
                    DisplayName = preset.DisplayName,
                    ProviderPreset = preset.Key,
                    Protocol = preset.Protocol,
                    BaseUrl = GetConfiguredBaseUrl(preset, seedOptions),
                    ModelId = GetConfiguredModelId(preset, seedOptions),
                    IsEnabled = true,
                    IsDefault = seedOptions.HasDefaultPreset
                        ? isConfiguredDefault
                        : preset.Key == TenantModelConnectorPresetCatalog.Mimo
                };

                var apiKey = GetConfiguredApiKey(preset, seedOptions);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    connector.EncryptedApiKey = _secretProtector.Protect(apiKey);
                }

                tenant.ModelConnectors.Add(connector);
                continue;
            }

            connector.DisplayName = string.IsNullOrWhiteSpace(connector.DisplayName) ? preset.DisplayName : connector.DisplayName;
            if (string.IsNullOrWhiteSpace(connector.BaseUrl) || isConfiguredDefault && seedOptions.OverwriteFromEnvironment && seedOptions.HasEndpoint)
            {
                connector.BaseUrl = GetConfiguredBaseUrl(preset, seedOptions);
            }

            if (string.IsNullOrWhiteSpace(connector.ModelId) || isConfiguredDefault && seedOptions.OverwriteFromEnvironment && seedOptions.HasModelId)
            {
                connector.ModelId = GetConfiguredModelId(preset, seedOptions);
            }

            connector.Protocol = connector.Protocol == default ? preset.Protocol : connector.Protocol;
            connector.ProviderPreset = string.IsNullOrWhiteSpace(connector.ProviderPreset) ? preset.Key : connector.ProviderPreset;

            var configuredApiKey = GetConfiguredApiKey(preset, seedOptions);
            if (!string.IsNullOrWhiteSpace(configuredApiKey)
                && (string.IsNullOrWhiteSpace(connector.EncryptedApiKey)
                    || isConfiguredDefault && seedOptions.OverwriteFromEnvironment))
            {
                connector.EncryptedApiKey = _secretProtector.Protect(configuredApiKey);
            }
        }

        if (seedOptions.HasDefaultPreset)
        {
            var configuredDefault = tenant.ModelConnectors.FirstOrDefault(item => seedOptions.IsDefaultPreset(item.ProviderPreset));
            if (configuredDefault is not null)
            {
                foreach (var connector in tenant.ModelConnectors)
                {
                    connector.IsDefault = connector.Id == configuredDefault.Id;
                }

                configuredDefault.IsEnabled = true;
            }
        }

        if (!tenant.ModelConnectors.Any(item => item.IsDefault))
        {
            var fallback = tenant.ModelConnectors.FirstOrDefault(item => item.ProviderPreset == TenantModelConnectorPresetCatalog.Mimo)
                ?? tenant.ModelConnectors.FirstOrDefault();
            if (fallback is not null)
            {
                fallback.IsDefault = true;
                fallback.IsEnabled = true;
            }
        }

        tenant.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    private string GetConfiguredBaseUrl(TenantModelConnectorPreset preset, ModelConnectorSeedOptions seedOptions)
    {
        if (seedOptions.IsDefaultPreset(preset) && !string.IsNullOrWhiteSpace(seedOptions.Endpoint))
        {
            return Normalize(seedOptions.Endpoint);
        }

        return Normalize(GetLegacyConfigurationValue(preset, "Endpoint") ?? preset.BaseUrl);
    }

    private string GetConfiguredModelId(TenantModelConnectorPreset preset, ModelConnectorSeedOptions seedOptions)
    {
        if (seedOptions.IsDefaultPreset(preset) && !string.IsNullOrWhiteSpace(seedOptions.ModelId))
        {
            return Normalize(seedOptions.ModelId);
        }

        return Normalize(GetLegacyConfigurationValue(preset, "ModelId") ?? preset.ModelId);
    }

    private string? GetConfiguredApiKey(TenantModelConnectorPreset preset, ModelConnectorSeedOptions seedOptions)
    {
        if (seedOptions.IsDefaultPreset(preset) && !string.IsNullOrWhiteSpace(seedOptions.ApiKey))
        {
            return NormalizeOrNull(seedOptions.ApiKey);
        }

        return NormalizeOrNull(GetLegacyConfigurationValue(preset, "ApiKey"));
    }

    private string? GetLegacyConfigurationValue(TenantModelConnectorPreset preset, string key)
    {
        foreach (var section in preset.GetConfigurationSections())
        {
            var value = _configuration[$"{section}:{key}"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string Normalize(string value) => value.Trim().TrimEnd('/');

    private static string? NormalizeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record ModelConnectorSeedOptions(
        string? DefaultPreset,
        string? ApiKey,
        string? Endpoint,
        string? ModelId,
        bool OverwriteFromEnvironment)
    {
        public bool HasDefaultPreset => !string.IsNullOrWhiteSpace(DefaultPreset);
        public bool HasEndpoint => !string.IsNullOrWhiteSpace(Endpoint);
        public bool HasModelId => !string.IsNullOrWhiteSpace(ModelId);

        public static ModelConnectorSeedOptions FromConfiguration(IConfiguration configuration)
        {
            var defaultPreset = NormalizeOrNull(configuration["ModelConnector:DefaultPreset"]);
            if (!string.IsNullOrWhiteSpace(defaultPreset))
            {
                if (!TenantModelConnectorPresetCatalog.TryGet(defaultPreset, out var preset))
                {
                    throw new InvalidOperationException($"Unknown model connector preset '{defaultPreset}'.");
                }

                defaultPreset = preset.Key;
            }

            return new ModelConnectorSeedOptions(
                defaultPreset,
                NormalizeOrNull(configuration["ModelConnector:ApiKey"]),
                NormalizeOrNull(configuration["ModelConnector:Endpoint"]),
                NormalizeOrNull(configuration["ModelConnector:ModelId"]),
                configuration.GetValue("ModelConnector:OverwriteFromEnvironment", false));
        }

        public bool IsDefaultPreset(TenantModelConnectorPreset preset)
            => IsDefaultPreset(preset.Key);

        public bool IsDefaultPreset(string providerPreset)
            => !string.IsNullOrWhiteSpace(DefaultPreset)
                && string.Equals(providerPreset, DefaultPreset, StringComparison.OrdinalIgnoreCase);
    }
}
