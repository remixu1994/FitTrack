using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IFitnessService
{
    // 健身目标相关
    Task<FitnessGoal> CreateFitnessGoalAsync(FitnessGoal goal, CancellationToken ct = default);
    Task<FitnessGoal?> GetFitnessGoalAsync(int id, CancellationToken ct = default);
    Task<List<FitnessGoal>> GetFitnessGoalsByUserIdAsync(string userId, CancellationToken ct = default);
    Task<FitnessGoal> UpdateFitnessGoalAsync(FitnessGoal goal, CancellationToken ct = default);
    Task DeleteFitnessGoalAsync(int id, CancellationToken ct = default);
    
    // 健身计划相关
    Task<WorkoutPlan> CreateWorkoutPlanAsync(WorkoutPlan plan, CancellationToken ct = default);
    Task<WorkoutPlan?> GetWorkoutPlanAsync(int id, CancellationToken ct = default);
    Task<List<WorkoutPlan>> GetWorkoutPlansByUserIdAsync(string userId, CancellationToken ct = default);
    Task<WorkoutPlan> UpdateWorkoutPlanAsync(WorkoutPlan plan, CancellationToken ct = default);
    Task DeleteWorkoutPlanAsync(int id, CancellationToken ct = default);
}
