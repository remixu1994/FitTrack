using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IAuthTokenService
{
    Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken)> IssueAsync(ApplicationUser user, CancellationToken ct = default);
    Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, ApplicationUser User)?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}
