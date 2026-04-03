namespace FitTrack.Copilot.Api.Contracts;

public record ExerciseSessionRequest(string ExerciseName, int? Sets, int? Reps, int? Duration);

public record CreateWorkoutSessionRequest(
    int? WorkoutPlanId,
    DateTime StartTime,
    DateTime EndTime,
    double? CaloriesBurned,
    string? Notes,
    IReadOnlyList<ExerciseSessionRequest>? Exercises);

public record WorkoutSessionDto(
    int Id,
    DateTime StartTime,
    DateTime EndTime,
    int? Duration,
    double? CaloriesBurned,
    string? Notes);

public record ExerciseDto(string Name, string? Description, int? Sets, int? Reps, int? Duration, int? RestTime);

public record WorkoutDayDto(string DayOfWeek, IReadOnlyList<ExerciseDto> Exercises);

public record WorkoutPlanDto(int Id, string PlanName, string? Description, string? FitnessLevel, DateTime CreatedAt, IReadOnlyList<WorkoutDayDto> WorkoutDays);

public record CreateWorkoutPlanRequest(string PlanName, string? Description, string? FitnessLevel, IReadOnlyList<CreateWorkoutDayRequest>? WorkoutDays);

public record CreateWorkoutDayRequest(string DayOfWeek, IReadOnlyList<CreateExerciseRequest>? Exercises);

public record CreateExerciseRequest(string Name, string? Description, int? Sets, int? Reps, int? Duration, int? RestTime);
