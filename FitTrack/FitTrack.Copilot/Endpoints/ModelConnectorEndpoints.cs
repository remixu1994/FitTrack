using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class ModelConnectorEndpoints
{
    public static IEndpointRouteBuilder MapModelConnectorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/model-connectors").WithTags("Model Connectors").RequireAuthorization();

        group.MapGet("/", async (HttpContext httpContext, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            var connectors = await connectorService.ListAvailableConnectorsForUserAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(new ApiResponse<IReadOnlyList<TenantModelConnectorOptionDto>>(true, connectors));
        });

        return app;
    }
}
