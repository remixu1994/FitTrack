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

        group.MapPut("/", async (HttpContext httpContext, UpsertUserProfileRequest request, IProfileService profileService, CancellationToken ct) =>
        {
            var profile = await profileService.UpdateAsync(
                httpContext.User.GetRequiredUserId(),
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
                },
                httpContext.User.GetEmail(),
                ct);

            return Results.Ok(new ApiResponse<UserProfileDto>(true, profile.ToDto()));
        });

        return app;
    }
}
