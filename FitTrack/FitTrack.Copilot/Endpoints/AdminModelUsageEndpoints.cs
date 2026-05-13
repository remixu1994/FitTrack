using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class AdminModelUsageEndpoints
{
    public static IEndpointRouteBuilder MapAdminModelUsageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin Model Usage")
            .RequireAuthorization(policy => policy.RequireRole(TenantConstants.AdminRole));

        group.MapGet("/tenants/{tenantId}/model-usage/overview", async (
            string tenantId,
            string? range,
            IModelUsageService modelUsageService,
            CancellationToken ct) =>
        {
            try
            {
                var overview = await modelUsageService.GetOverviewAsync(tenantId, range ?? "24h", ct);
                return Results.Ok(new ApiResponse<ModelUsageOverviewDto>(true, overview));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapGet("/tenants/{tenantId}/model-usage/charts", async (
            string tenantId,
            string? range,
            IModelUsageService modelUsageService,
            CancellationToken ct) =>
        {
            try
            {
                var charts = await modelUsageService.GetChartsAsync(tenantId, range ?? "24h", ct);
                return Results.Ok(new ApiResponse<ModelUsageChartsDto>(true, charts));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapGet("/tenants/{tenantId}/model-request-logs", async (
            string tenantId,
            string? range,
            string? connectorId,
            string? modelSearch,
            string? status,
            string? requestType,
            int? page,
            int? pageSize,
            IModelUsageService modelUsageService,
            CancellationToken ct) =>
        {
            try
            {
                var logs = await modelUsageService.GetLogsAsync(
                    tenantId,
                    range ?? "24h",
                    connectorId,
                    modelSearch,
                    status,
                    requestType,
                    page ?? 1,
                    pageSize ?? 20,
                    ct);
                return Results.Ok(new ApiResponse<ModelRequestLogListDto>(true, logs));
            }
            catch (TenantModelConnectorValidationException ex)
            {
                return ToErrorResult(ex);
            }
        });

        group.MapPost("/tenants/{tenantId}/model-request-logs/cleanup", async (
            string tenantId,
            IModelUsageService modelUsageService,
            CancellationToken ct) =>
        {
            try
            {
                var result = await modelUsageService.CleanupAsync(tenantId, ct);
                return Results.Ok(new ApiResponse<ModelRequestLogCleanupDto>(true, result));
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
