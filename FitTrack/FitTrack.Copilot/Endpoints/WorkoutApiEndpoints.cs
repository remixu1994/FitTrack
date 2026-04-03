using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class WorkoutApiEndpoints
{
    public static IEndpointRouteBuilder MapWorkoutApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Workouts").RequireAuthorization();

        group.MapGet("/workout-plans", async (HttpContext httpContext, IFitnessService fitnessService, CancellationToken ct) =>
        {
            var plans = await fitnessService.GetWorkoutPlansByUserIdAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(new ApiResponse<IReadOnlyList<WorkoutPlanDto>>(true, plans.Select(MapPlan).ToList()));
        });

        group.MapPost("/workout-plans", async (HttpContext httpContext, CreateWorkoutPlanRequest request, IFitnessService fitnessService, CancellationToken ct) =>
        {
            var plan = new WorkoutPlan
            {
                UserId = httpContext.User.GetRequiredUserId(),
                PlanName = request.PlanName,
                Description = request.Description,
                FitnessLevel = request.FitnessLevel,
                WorkoutDays = request.WorkoutDays?.Select(day => new WorkoutDay
                {
                    DayOfWeek = day.DayOfWeek,
                    Exercises = day.Exercises?.Select(ex => new Exercise
                    {
                        Name = ex.Name,
                        Description = ex.Description,
                        Sets = ex.Sets,
                        Reps = ex.Reps,
                        Duration = ex.Duration,
                        RestTime = ex.RestTime
                    }).ToList() ?? new List<Exercise>()
                }).ToList() ?? new List<WorkoutDay>()
            };

            var created = await fitnessService.CreateWorkoutPlanAsync(plan, ct);
            return Results.Created($"/api/workout-plans/{created.Id}", new ApiResponse<WorkoutPlanDto>(true, MapPlan(created)));
        });

        group.MapGet("/workout-sessions", async (HttpContext httpContext, IWorkoutSessionService workoutSessionService, CancellationToken ct) =>
        {
            var sessions = await workoutSessionService.GetWorkoutSessionsByUserIdAsync(httpContext.User.GetRequiredUserId(), ct);
            var data = sessions.Select(s => new WorkoutSessionDto(s.Id, s.StartTime, s.EndTime, s.Duration, s.CaloriesBurned, s.Notes)).ToList();
            return Results.Ok(new ApiResponse<IReadOnlyList<WorkoutSessionDto>>(true, data));
        });

        group.MapPost("/workout-sessions", async (HttpContext httpContext, CreateWorkoutSessionRequest request, IWorkoutSessionService workoutSessionService, CancellationToken ct) =>
        {
            var session = new WorkoutSession
            {
                UserId = httpContext.User.GetRequiredUserId(),
                WorkoutPlanId = request.WorkoutPlanId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CaloriesBurned = request.CaloriesBurned,
                Notes = request.Notes,
                ExerciseSessions = request.Exercises?.Select(ex => new ExerciseSession
                {
                    ExerciseName = ex.ExerciseName,
                    Sets = ex.Sets,
                    Reps = ex.Reps,
                    Duration = ex.Duration
                }).ToList() ?? new List<ExerciseSession>()
            };

            var created = await workoutSessionService.CreateWorkoutSessionAsync(session, ct);
            var dto = new WorkoutSessionDto(created.Id, created.StartTime, created.EndTime, created.Duration, created.CaloriesBurned, created.Notes);
            return Results.Created($"/api/workout-sessions/{created.Id}", new ApiResponse<WorkoutSessionDto>(true, dto));
        });

        return app;
    }

    private static WorkoutPlanDto MapPlan(WorkoutPlan plan)
        => new(
            plan.Id,
            plan.PlanName,
            plan.Description,
            plan.FitnessLevel,
            plan.CreatedAt,
            plan.WorkoutDays.Select(day => new WorkoutDayDto(
                day.DayOfWeek,
                day.Exercises.Select(ex => new ExerciseDto(ex.Name, ex.Description, ex.Sets, ex.Reps, ex.Duration, ex.RestTime)).ToList()))
                .ToList());
}
