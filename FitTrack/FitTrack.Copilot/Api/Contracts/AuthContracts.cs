namespace FitTrack.Copilot.Api.Contracts;

public record LoginRequest(string Email, string Password);

public record RefreshResponse(string AccessToken, DateTime ExpiresAtUtc);

public record AuthenticatedUserDto(string Id, string Email, string? DisplayName, IReadOnlyList<string> Roles);

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, AuthenticatedUserDto User);
