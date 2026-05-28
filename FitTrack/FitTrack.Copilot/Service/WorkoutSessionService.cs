using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class WorkoutSessionService : IWorkoutSessionService
{
    private readonly ApplicationDbContext _dbContext;

    public WorkoutSessionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkoutSession> CreateWorkoutSessionAsync(WorkoutSession session, CancellationToken ct = default)
    {
        // 计算会话持续时间
        if (session.EndTime > session.StartTime)
        {
            session.Duration = (int)(session.EndTime - session.StartTime).TotalMinutes;
        }

        _dbContext.WorkoutSessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);
        return session;
    }

    public async Task<WorkoutSession?> GetWorkoutSessionAsync(int id, CancellationToken ct = default)
    {
        return await _dbContext.WorkoutSessions
            .Include(s => s.ExerciseSessions)
            .Include(s => s.WorkoutPlan)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<List<WorkoutSession>> GetWorkoutSessionsByUserIdAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.WorkoutSessions
            .Where(s => s.UserId == userId)
            .Include(s => s.ExerciseSessions)
            .OrderByDescending(s => s.StartTime)
            .ToListAsync(ct);
    }

    public async Task<WorkoutSession> UpdateWorkoutSessionAsync(WorkoutSession session, CancellationToken ct = default)
    {
        // 重新计算会话持续时间
        if (session.EndTime > session.StartTime)
        {
            session.Duration = (int)(session.EndTime - session.StartTime).TotalMinutes;
        }

        _dbContext.WorkoutSessions.Update(session);
        await _dbContext.SaveChangesAsync(ct);
        return session;
    }

    public async Task DeleteWorkoutSessionAsync(int id, CancellationToken ct = default)
    {
        var session = await _dbContext.WorkoutSessions.FindAsync(id, ct);
        if (session != null)
        {
            _dbContext.WorkoutSessions.Remove(session);
            await _dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<List<WorkoutSession>> GetWorkoutSessionsByDateRangeAsync(string userId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _dbContext.WorkoutSessions
            .Where(s => s.UserId == userId && s.StartTime >= startDate && s.StartTime <= endDate)
            .Include(s => s.ExerciseSessions)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);
    }
}
