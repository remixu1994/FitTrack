using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class FitnessService : IFitnessService
{
    private readonly ApplicationDbContext _dbContext;

    public FitnessService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // 健身目标相关
    public async Task<FitnessGoal> CreateFitnessGoalAsync(FitnessGoal goal, CancellationToken ct = default)
    {
        _dbContext.FitnessGoals.Add(goal);
        await _dbContext.SaveChangesAsync(ct);
        return goal;
    }

    public async Task<FitnessGoal?> GetFitnessGoalAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.FitnessGoals.FindAsync(id, ct);
    }

    public async Task<List<FitnessGoal>> GetFitnessGoalsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.FitnessGoals
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.StartDate)
            .ToListAsync(ct);
    }

    public async Task<FitnessGoal> UpdateFitnessGoalAsync(FitnessGoal goal, CancellationToken ct = default)
    {
        _dbContext.FitnessGoals.Update(goal);
        await _dbContext.SaveChangesAsync(ct);
        return goal;
    }

    public async Task DeleteFitnessGoalAsync(int id, CancellationToken ct = default)
    {
        var goal = await _dbContext.FitnessGoals.FindAsync(id, ct);
        if (goal != null)
        {
            _dbContext.FitnessGoals.Remove(goal);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    // 健身计划相关
    public async Task<WorkoutPlan> CreateWorkoutPlanAsync(WorkoutPlan plan, CancellationToken ct = default)
    {
        _dbContext.WorkoutPlans.Add(plan);
        await _dbContext.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<WorkoutPlan?> GetWorkoutPlanAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.WorkoutPlans
            .Include(p => p.WorkoutDays)
            .ThenInclude(d => d.Exercises)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<List<WorkoutPlan>> GetWorkoutPlansByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.WorkoutPlans
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<WorkoutPlan> UpdateWorkoutPlanAsync(WorkoutPlan plan, CancellationToken ct = default)
    {
        _dbContext.WorkoutPlans.Update(plan);
        await _dbContext.SaveChangesAsync(ct);
        return plan;
    }

    public async Task DeleteWorkoutPlanAsync(int id, CancellationToken ct = default)
    {
        var plan = await _dbContext.WorkoutPlans.FindAsync(id, ct);
        if (plan != null)
        {
            _dbContext.WorkoutPlans.Remove(plan);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
