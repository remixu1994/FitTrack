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
                    BaseUrl = GetConfiguredBaseUrl(preset),
                    ModelId = GetConfiguredModelId(preset),
                    IsEnabled = true,
                    IsDefault = preset.Key == TenantModelConnectorPresetCatalog.XiaomiMimo
                };

                var apiKey = GetConfiguredApiKey(preset);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    connector.EncryptedApiKey = _secretProtector.Protect(apiKey);
                }

                tenant.ModelConnectors.Add(connector);
                continue;
            }

            connector.DisplayName = string.IsNullOrWhiteSpace(connector.DisplayName) ? preset.DisplayName : connector.DisplayName;
            connector.BaseUrl = string.IsNullOrWhiteSpace(connector.BaseUrl) ? GetConfiguredBaseUrl(preset) : connector.BaseUrl;
            connector.ModelId = string.IsNullOrWhiteSpace(connector.ModelId) ? GetConfiguredModelId(preset) : connector.ModelId;
            connector.Protocol = connector.Protocol == default ? preset.Protocol : connector.Protocol;
            connector.ProviderPreset = string.IsNullOrWhiteSpace(connector.ProviderPreset) ? preset.Key : connector.ProviderPreset;

            if (string.IsNullOrWhiteSpace(connector.EncryptedApiKey))
            {
                var apiKey = GetConfiguredApiKey(preset);
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    connector.EncryptedApiKey = _secretProtector.Protect(apiKey);
                }
            }
        }

        if (!tenant.ModelConnectors.Any(item => item.IsDefault))
        {
            var fallback = tenant.ModelConnectors.FirstOrDefault(item => item.ProviderPreset == TenantModelConnectorPresetCatalog.XiaomiMimo)
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

    private string GetConfiguredBaseUrl(TenantModelConnectorPreset preset)
        => Normalize(_configuration[$"{preset.ConfigurationSection}:Endpoint"] ?? preset.BaseUrl);

    private string GetConfiguredModelId(TenantModelConnectorPreset preset)
        => Normalize(_configuration[$"{preset.ConfigurationSection}:ModelId"] ?? preset.ModelId);

    private string? GetConfiguredApiKey(TenantModelConnectorPreset preset)
        => preset.ConfigurationSection is null
            ? null
            : NormalizeOrNull(_configuration[$"{preset.ConfigurationSection}:ApiKey"]);

    private static string Normalize(string value) => value.Trim().TrimEnd('/');

    private static string? NormalizeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
