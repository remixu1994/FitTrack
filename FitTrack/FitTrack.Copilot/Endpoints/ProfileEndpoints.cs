using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile").WithTags("Profile").RequireAuthorization();

        group.MapGet("/", async (HttpContext httpContext, IProfileService profileService, CancellationToken ct) =>
        {
            var profile = await profileService.GetOrCreateProfileAsync(httpContext.User.GetRequiredUserId(), httpContext.User.GetEmail(), ct);
            return Results.Ok(new ApiResponse<UserProfileDto>(true, profile.ToDto()));
        });

        group.MapPut("/", async (HttpContext httpContext, UpsertUserProfileRequest request, IProfileService profileService, ITenantModelConnectorService connectorService, CancellationToken ct) =>
        {
            var userId = httpContext.User.GetRequiredUserId();
            if (!string.IsNullOrWhiteSpace(request.PreferredModelConnectorId) &&
                !await connectorService.CanUseConnectorAsync(userId, request.PreferredModelConnectorId, ct))
            {
                return Results.Json(
                    new ApiResponse<object>(
                        false,
                        Error: new ApiError(
                            "INVALID_MODEL_CONNECTOR",
                            $"The connector '{request.PreferredModelConnectorId}' is not available for the current tenant.")),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var profile = await profileService.UpdateAsync(
                userId,
                entity =>
                {
                    entity.DisplayName = request.DisplayName;
                    entity.Sex = request.Sex;
                    entity.Age = request.Age;
                    entity.HeightCm = request.HeightCm;
                    entity.WeightKg = request.WeightKg;
                    entity.BodyFatPercent = request.BodyFatPercent;
                    entity.ActivityLevel = request.ActivityLevel;
                    entity.Goal = request.Goal;
                    entity.Preferences = request.Preferences;
                    entity.PreferredModelConnectorId = string.IsNullOrWhiteSpace(request.PreferredModelConnectorId)
                        ? null
                        : request.PreferredModelConnectorId.Trim();
                },
                httpContext.User.GetEmail(),
                ct);

            return Results.Ok(new ApiResponse<UserProfileDto>(true, profile.ToDto()));
        });

        return app;
    }
}
