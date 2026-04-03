using System.Net;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class InternalAgentToolEndpoints
{
    public static IEndpointRouteBuilder MapInternalAgentToolEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/internal/agent-tools")
            .WithTags("InternalAgentTools")
            .AddEndpointFilter(new LoopbackOnlyEndpointFilter());

        group.MapGet("/progress/summary", async (string userId, IProgressService progressService, CancellationToken ct) =>
            Results.Ok(await progressService.GetSummaryAsync(userId, ct)))
            .ExcludeFromDescription();

        group.MapGet("/nutrition/daily-snapshot", async (string userId, INutritionTools nutritionTools, CancellationToken ct) =>
            Results.Ok(await nutritionTools.BuildDailyNutritionSnapshotAsync(userId, ct)))
            .ExcludeFromDescription();

        group.MapGet("/workouts/recent", async (string userId, IWorkoutSessionService workoutSessionService, CancellationToken ct) =>
        {
            var sessions = await workoutSessionService.GetWorkoutSessionsByUserIdAsync(userId, ct);
            var data = sessions
                .Take(5)
                .Select(session => new RecentWorkoutDto(
                    session.Id,
                    session.StartTime,
                    session.EndTime,
                    session.Duration,
                    session.CaloriesBurned,
                    session.Notes,
                    session.ExerciseSessions
                        .Select(exercise => new RecentWorkoutExerciseDto(exercise.ExerciseName, exercise.Sets, exercise.Reps, exercise.Duration))
                        .ToList()))
                .ToList();

            return Results.Ok(data);
        }).ExcludeFromDescription();

        group.MapPost("/nutrition/analyze-text", async (AnalyzeTextRequest request, IFoodAiService foodAiService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return Results.BadRequest(new ApiResponse<object>(false, Error: new ApiError("TEXT_REQUIRED", "Text is required.")));
            }

            var result = await foodAiService.AnalyzeAsync(new FoodRequest
            {
                UserId = request.UserId,
                Text = request.Text.Trim()
            }, ct);

            return Results.Ok(result);
        }).ExcludeFromDescription();

        return app;
    }

    private sealed class LoopbackOnlyEndpointFilter : IEndpointFilter
    {
        public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
            if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
            {
                return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status403Forbidden));
            }

            return next(context);
        }
    }

    private sealed record AnalyzeTextRequest(string UserId, string Text);

    private sealed record RecentWorkoutDto(
        int Id,
        DateTime StartTime,
        DateTime EndTime,
        int? Duration,
        double? CaloriesBurned,
        string? Notes,
        IReadOnlyList<RecentWorkoutExerciseDto> Exercises);

    private sealed record RecentWorkoutExerciseDto(
        string ExerciseName,
        int Sets,
        int Reps,
        int Duration);
}
