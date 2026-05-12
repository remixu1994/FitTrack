using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public sealed class TenantModelConnectorService : ITenantModelConnectorService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConnectorSecretProtector _secretProtector;

    public TenantModelConnectorService(
        ApplicationDbContext dbContext,
        IConnectorSecretProtector secretProtector)
    {
        _dbContext = dbContext;
        _secretProtector = secretProtector;
    }

    public async Task<IReadOnlyList<TenantSummaryDto>> ListTenantsAsync(CancellationToken ct = default)
    {
        var tenants = await _dbContext.Tenants
            .AsNoTracking()
            .Include(item => item.Users)
            .Include(item => item.ModelConnectors)
            .OrderBy(item => item.Name)
            .ToListAsync(ct);

        return tenants.Select(item => item.ToDto()).ToList();
    }

    public Task<IReadOnlyList<TenantModelConnectorPresetDto>> ListPresetsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TenantModelConnectorPresetDto>>(TenantModelConnectorPresetCatalog.All.Select(item => item.ToDto()).ToList());

    public async Task<IReadOnlyList<TenantModelConnectorAdminDto>> ListTenantConnectorsAsync(string tenantId, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);

        var connectors = await _dbContext.TenantModelConnectors
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(ct);

        return connectors.Select(item => item.ToAdminDto()).ToList();
    }

    public async Task<TenantModelConnectorAdminDto> CreateConnectorAsync(string tenantId, UpsertTenantModelConnectorRequest request, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);
        var normalized = Normalize(request);

        var connector = new TenantModelConnector
        {
            TenantId = tenantId,
            DisplayName = normalized.DisplayName,
            ProviderPreset = normalized.ProviderPreset,
            Protocol = normalized.Protocol,
            BaseUrl = normalized.BaseUrl,
            ModelId = normalized.ModelId,
            IsDefault = false,
            IsEnabled = normalized.IsEnabled,
            EncryptedApiKey = string.IsNullOrWhiteSpace(normalized.ApiKey)
                ? null
                : _secretProtector.Protect(normalized.ApiKey)
        };

        _dbContext.TenantModelConnectors.Add(connector);
        await _dbContext.SaveChangesAsync(ct);

        if (normalized.IsDefault || !await HasDefaultConnectorAsync(tenantId, connector.Id, ct))
        {
            await SetDefaultInternalAsync(tenantId, connector.Id, ct);
        }

        return connector.ToAdminDto();
    }

    public async Task<TenantModelConnectorAdminDto> UpdateConnectorAsync(string tenantId, string connectorId, UpsertTenantModelConnectorRequest request, CancellationToken ct = default)
    {
        var connector = await GetTenantConnectorAsync(tenantId, connectorId, ct);
        var normalized = Normalize(request);

        if (connector.IsDefault && !normalized.IsEnabled)
        {
            throw new TenantModelConnectorValidationException("DEFAULT_CONNECTOR_DISABLED", "Set another enabled connector as default before disabling the current default connector.");
        }

        connector.DisplayName = normalized.DisplayName;
        connector.ProviderPreset = normalized.ProviderPreset;
        connector.Protocol = normalized.Protocol;
        connector.BaseUrl = normalized.BaseUrl;
        connector.ModelId = normalized.ModelId;
        connector.IsEnabled = normalized.IsEnabled;
        connector.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(normalized.ApiKey))
        {
            connector.EncryptedApiKey = _secretProtector.Protect(normalized.ApiKey);
        }

        await _dbContext.SaveChangesAsync(ct);

        if (normalized.IsDefault)
        {
            await SetDefaultInternalAsync(tenantId, connector.Id, ct);
        }

        return connector.ToAdminDto();
    }

    public async Task DeleteConnectorAsync(string tenantId, string connectorId, CancellationToken ct = default)
    {
        var connector = await GetTenantConnectorAsync(tenantId, connectorId, ct);
        var tenantConnectors = await _dbContext.TenantModelConnectors
            .Where(item => item.TenantId == tenantId)
            .OrderByDescending(item => item.IsEnabled)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(ct);

        if (tenantConnectors.Count == 1)
        {
            throw new TenantModelConnectorValidationException("LAST_CONNECTOR_DELETE_BLOCKED", "At least one model connector must remain for a tenant.");
        }

        if (connector.IsDefault)
        {
            var replacement = tenantConnectors.FirstOrDefault(item => item.Id != connectorId && item.IsEnabled);
            if (replacement is null)
            {
                throw new TenantModelConnectorValidationException("DEFAULT_CONNECTOR_DELETE_BLOCKED", "Enable another connector before deleting the current default connector.");
            }

            replacement.IsDefault = true;
            replacement.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.TenantModelConnectors.Remove(connector);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<TenantModelConnectorAdminDto> SetDefaultConnectorAsync(string tenantId, string connectorId, CancellationToken ct = default)
    {
        var connector = await GetTenantConnectorAsync(tenantId, connectorId, ct);
        await SetDefaultInternalAsync(tenantId, connectorId, ct);
        return connector.ToAdminDto();
    }

    public async Task<IReadOnlyList<TenantModelConnectorOptionDto>> ListAvailableConnectorsForUserAsync(string userId, CancellationToken ct = default)
    {
        var tenantId = await GetTenantIdForUserAsync(userId, ct);
        var connectors = await _dbContext.TenantModelConnectors
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.IsEnabled)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(ct);

        return connectors.Select(item => item.ToOptionDto()).ToList();
    }

    public async Task<TenantModelConnector?> ResolveConnectorForUserAsync(string userId, CancellationToken ct = default)
    {
        var userProjection = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new
            {
                item.TenantId,
                PreferredModelConnectorId = item.Profile == null ? null : item.Profile.PreferredModelConnectorId
            })
            .FirstOrDefaultAsync(ct);

        if (userProjection is null || string.IsNullOrWhiteSpace(userProjection.TenantId))
        {
            return null;
        }

        var connectors = await _dbContext.TenantModelConnectors
            .AsNoTracking()
            .Where(item => item.TenantId == userProjection.TenantId && item.IsEnabled)
            .OrderByDescending(item => item.IsDefault)
            .ThenBy(item => item.DisplayName)
            .ToListAsync(ct);

        if (connectors.Count == 0)
        {
            return null;
        }

        return connectors.FirstOrDefault(item => item.Id == userProjection.PreferredModelConnectorId)
            ?? connectors.FirstOrDefault(item => item.IsDefault)
            ?? connectors[0];
    }

    public async Task<bool> CanUseConnectorAsync(string userId, string connectorId, CancellationToken ct = default)
    {
        var tenantId = await GetTenantIdForUserAsync(userId, ct);
        return await _dbContext.TenantModelConnectors.AnyAsync(
            item => item.Id == connectorId && item.TenantId == tenantId && item.IsEnabled,
            ct);
    }

    private async Task<string> GetTenantIdForUserAsync(string userId, CancellationToken ct)
    {
        var tenantId = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => item.TenantId)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new TenantModelConnectorValidationException("TENANT_NOT_FOUND", $"User '{userId}' does not belong to a tenant.", System.Net.HttpStatusCode.NotFound);
        }

        return tenantId;
    }

    private async Task<TenantModelConnector> GetTenantConnectorAsync(string tenantId, string connectorId, CancellationToken ct)
    {
        var connector = await _dbContext.TenantModelConnectors
            .FirstOrDefaultAsync(item => item.Id == connectorId && item.TenantId == tenantId, ct);

        return connector ?? throw new TenantModelConnectorValidationException("CONNECTOR_NOT_FOUND", $"Connector '{connectorId}' was not found for tenant '{tenantId}'.", System.Net.HttpStatusCode.NotFound);
    }

    private async Task EnsureTenantExistsAsync(string tenantId, CancellationToken ct)
    {
        var exists = await _dbContext.Tenants.AnyAsync(item => item.Id == tenantId, ct);
        if (!exists)
        {
            throw new TenantModelConnectorValidationException("TENANT_NOT_FOUND", $"Tenant '{tenantId}' was not found.", System.Net.HttpStatusCode.NotFound);
        }
    }

    private async Task<bool> HasDefaultConnectorAsync(string tenantId, string connectorId, CancellationToken ct)
        => await _dbContext.TenantModelConnectors.AnyAsync(item => item.TenantId == tenantId && item.IsDefault && item.Id != connectorId, ct);

    private async Task SetDefaultInternalAsync(string tenantId, string connectorId, CancellationToken ct)
    {
        var connectors = await _dbContext.TenantModelConnectors
            .Where(item => item.TenantId == tenantId)
            .ToListAsync(ct);

        var target = connectors.FirstOrDefault(item => item.Id == connectorId)
            ?? throw new TenantModelConnectorValidationException("CONNECTOR_NOT_FOUND", $"Connector '{connectorId}' was not found for tenant '{tenantId}'.", System.Net.HttpStatusCode.NotFound);

        if (!target.IsEnabled)
        {
            throw new TenantModelConnectorValidationException("DEFAULT_CONNECTOR_DISABLED", "A default connector must be enabled.");
        }

        foreach (var connector in connectors)
        {
            connector.IsDefault = connector.Id == connectorId;
            connector.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);
    }

    private static NormalizedConnectorRequest Normalize(UpsertTenantModelConnectorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new TenantModelConnectorValidationException("DISPLAY_NAME_REQUIRED", "Display name is required.");
        }

        if (!TenantModelConnectorPresetCatalog.TryGet(request.ProviderPreset, out var preset))
        {
            throw new TenantModelConnectorValidationException("PRESET_NOT_SUPPORTED", $"Unsupported model connector preset '{request.ProviderPreset}'.");
        }

        if (!Enum.TryParse<TenantModelProtocol>(request.Protocol?.Trim(), true, out var protocol))
        {
            throw new TenantModelConnectorValidationException("PROTOCOL_NOT_SUPPORTED", $"Unsupported model connector protocol '{request.Protocol}'.");
        }

        if (protocol != preset.Protocol)
        {
            throw new TenantModelConnectorValidationException("PRESET_PROTOCOL_MISMATCH", $"Preset '{preset.DisplayName}' must use protocol '{preset.Protocol}'.");
        }

        if (string.IsNullOrWhiteSpace(request.BaseUrl))
        {
            throw new TenantModelConnectorValidationException("BASE_URL_REQUIRED", "Base URL is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ModelId))
        {
            throw new TenantModelConnectorValidationException("MODEL_ID_REQUIRED", "Model ID is required.");
        }

        if (request.IsDefault && !request.IsEnabled)
        {
            throw new TenantModelConnectorValidationException("DEFAULT_CONNECTOR_DISABLED", "A default connector must be enabled.");
        }

        return new NormalizedConnectorRequest(
            request.DisplayName.Trim(),
            preset.Key,
            protocol,
            request.BaseUrl.Trim().TrimEnd('/'),
            request.ModelId.Trim(),
            string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey.Trim(),
            request.IsDefault,
            request.IsEnabled);
    }

    private sealed record NormalizedConnectorRequest(
        string DisplayName,
        string ProviderPreset,
        TenantModelProtocol Protocol,
        string BaseUrl,
        string ModelId,
        string? ApiKey,
        bool IsDefault,
        bool IsEnabled);
}
