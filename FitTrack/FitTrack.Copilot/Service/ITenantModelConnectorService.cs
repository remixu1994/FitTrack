using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface ITenantModelConnectorService
{
    Task<IReadOnlyList<TenantSummaryDto>> ListTenantsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantModelConnectorPresetDto>> ListPresetsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantModelConnectorAdminDto>> ListTenantConnectorsAsync(string tenantId, CancellationToken ct = default);
    Task<TenantModelConnectorAdminDto> CreateConnectorAsync(string tenantId, UpsertTenantModelConnectorRequest request, CancellationToken ct = default);
    Task<TenantModelConnectorAdminDto> UpdateConnectorAsync(string tenantId, string connectorId, UpsertTenantModelConnectorRequest request, CancellationToken ct = default);
    Task DeleteConnectorAsync(string tenantId, string connectorId, CancellationToken ct = default);
    Task<TenantModelConnectorAdminDto> SetDefaultConnectorAsync(string tenantId, string connectorId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantModelConnectorOptionDto>> ListAvailableConnectorsForUserAsync(string userId, CancellationToken ct = default);
    Task<TenantModelConnector?> ResolveConnectorForUserAsync(string userId, CancellationToken ct = default);
    Task<bool> CanUseConnectorAsync(string userId, string connectorId, CancellationToken ct = default);
}
