using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IProfileService
{
    Task<UserProfile> GetOrCreateProfileAsync(string userId, string? email = null, CancellationToken ct = default);
    Task<UserProfile> UpdateAsync(string userId, Action<UserProfile> applyChanges, string? email = null, CancellationToken ct = default);
}
