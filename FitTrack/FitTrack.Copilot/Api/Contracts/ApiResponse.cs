namespace FitTrack.Copilot.Api.Contracts;

public record ApiResponse<T>(bool Success, T? Data = default, ApiError? Error = null);

public record ApiError(string Code, string Message, object? Details = null);
