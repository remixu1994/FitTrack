using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/progress/summary", async (HttpContext httpContext, IProgressService progressService, CancellationToken ct) =>
        {
            var summary = await progressService.GetSummaryAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(new ApiResponse<ProgressSummaryDto>(true, summary));
        }).WithTags("Progress").RequireAuthorization();

        return app;
    }
}
