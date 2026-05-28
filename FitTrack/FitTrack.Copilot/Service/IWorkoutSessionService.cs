using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IWorkoutSessionService
{
    Task<WorkoutSession> CreateWorkoutSessionAsync(WorkoutSession session, CancellationToken ct = default);
    Task<WorkoutSession?> GetWorkoutSessionAsync(int id, CancellationToken ct = default);
    Task<List<WorkoutSession>> GetWorkoutSessionsByUserIdAsync(string userId, CancellationToken ct = default);
    Task<WorkoutSession> UpdateWorkoutSessionAsync(WorkoutSession session, CancellationToken ct = default);
    Task DeleteWorkoutSessionAsync(int id, CancellationToken ct = default);
    Task<List<WorkoutSession>> GetWorkoutSessionsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken ct = default);
}
