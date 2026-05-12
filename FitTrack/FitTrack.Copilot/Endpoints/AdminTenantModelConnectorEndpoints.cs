using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class AdminTenantModelConnectorEndpoints
{
    public static IEndpointRouteBuilder MapAdminTenantModelConnectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin Tenant Model Connectors")
            .RequireAuthorization(policy => policy.RequireRole(TenantConstants.AdminRole));

        group.MapGet("/tenants", async (ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            var tenants = await connectorService.ListTenantsAsync(ct);
            return Results.Ok(new ApiResponse<IReadOnlyList<TenantSummaryDto>>(true, tenants));
        });

        group.MapGet("/model-connector-presets", async (ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            var presets = await connectorService.ListPresetsAsync(ct);
            return Results.Ok(new ApiResponse<IReadOnlyList<TenantModelConnectorPresetDto>>(true, presets));
        });

        group.MapGet("/tenants/{tenantId}/model-connectors", async (string tenantId, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            try
            {
                var connectors = await connectorService.ListTenantConnectorsAsync(tenantId, ct);
                return Results.Ok(new ApiResponse<IReadOnlyList<TenantModelConnectorAdminDto>>(true, connectors));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapPost("/tenants/{tenantId}/model-connectors", async (string tenantId, UpsertTenantModelConnectorRequest request, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            try
            {
                var connector = await connectorService.CreateConnectorAsync(tenantId, request, ct);
                return Results.Ok(new ApiResponse<TenantModelConnectorAdminDto>(true, connector));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapPut("/tenants/{tenantId}/model-connectors/{connectorId}", async (string tenantId, string connectorId, UpsertTenantModelConnectorRequest request, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            try
            {
                var connector = await connectorService.UpdateConnectorAsync(tenantId, connectorId, request, ct);
                return Results.Ok(new ApiResponse<TenantModelConnectorAdminDto>(true, connector));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapDelete("/tenants/{tenantId}/model-connectors/{connectorId}", async (string tenantId, string connectorId, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            try
            {
                await connectorService.DeleteConnectorAsync(tenantId, connectorId, ct);
                return Results.Ok(new ApiResponse<object>(true, new { Deleted = true }));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapPost("/tenants/{tenantId}/model-connectors/{connectorId}/default", async (string tenantId, string connectorId, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            try
            {
                var connector = await connectorService.SetDefaultConnectorAsync(tenantId, connectorId, ct);
                return Results.Ok(new ApiResponse<TenantModelConnectorAdminDto>(true, connector));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        return app;
    }

    private static IResult ToErrorResult(TenantModelConnectorValidationException ex)
        => Results.Json(new ApiResponse<object>(false, Error: new ApiError(ex.ErrorCode, ex.Message)), statusCode: (int)ex.StatusCode);
}
