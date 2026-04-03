using FitTrack.Copilot.Data;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Copilot.Service;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _dbContext;

    public ProfileService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserProfile> GetOrCreateProfileAsync(string userId, string? email = null, CancellationToken ct = default)
    {
        var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile is not null)
        {
            return profile;
        }

        profile = new UserProfile
        {
            UserId = userId,
            DisplayName = email
        };

        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(ct);
        return profile;
    }

    public async Task<UserProfile> UpdateAsync(string userId, Action<UserProfile> applyChanges, string? email = null, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, email, ct);
        applyChanges(profile);
        profile.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return profile;
    }
}
