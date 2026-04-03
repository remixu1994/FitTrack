using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Configuration;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FitTrack.Copilot.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            IAuthTokenService tokenService,
            IProfileService profileService,
            IOptions<JwtOptions> jwtOptions,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                return Results.Json(new ApiResponse<object>(false, Error: new ApiError("INVALID_CREDENTIALS", "Invalid email or password.")), statusCode: StatusCodes.Status401Unauthorized);
            }

            var profile = await profileService.GetOrCreateProfileAsync(user.Id, user.Email, ct);
            var tokens = await tokenService.IssueAsync(user, ct);
            WriteRefreshCookie(httpContext, jwtOptions.Value, tokens.RefreshToken);
            return Results.Ok(new ApiResponse<AuthResponse>(true, new AuthResponse(tokens.AccessToken, tokens.ExpiresAtUtc, user.ToDto(profile))));
        });

        group.MapPost("/refresh", async (
            HttpContext httpContext,
            IAuthTokenService tokenService,
            IProfileService profileService,
            IOptions<JwtOptions> jwtOptions,
            CancellationToken ct) =>
        {
            if (!httpContext.Request.Cookies.TryGetValue(jwtOptions.Value.RefreshCookieName, out var refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return Results.Json(new ApiResponse<object>(false, Error: new ApiError("REFRESH_TOKEN_MISSING", "Refresh token cookie is missing.")), statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await tokenService.RefreshAsync(refreshToken, ct);
            if (result is null)
            {
                return Results.Json(new ApiResponse<object>(false, Error: new ApiError("REFRESH_TOKEN_INVALID", "Refresh token is invalid or expired.")), statusCode: StatusCodes.Status401Unauthorized);
            }

            var profile = await profileService.GetOrCreateProfileAsync(result.Value.User.Id, result.Value.User.Email, ct);
            WriteRefreshCookie(httpContext, jwtOptions.Value, result.Value.RefreshToken);
            return Results.Ok(new ApiResponse<AuthResponse>(true, new AuthResponse(result.Value.AccessToken, result.Value.ExpiresAtUtc, result.Value.User.ToDto(profile))));
        });

        group.MapPost("/logout", async (
            HttpContext httpContext,
            IAuthTokenService tokenService,
            IOptions<JwtOptions> jwtOptions,
            CancellationToken ct) =>
        {
            if (httpContext.Request.Cookies.TryGetValue(jwtOptions.Value.RefreshCookieName, out var refreshToken) && !string.IsNullOrWhiteSpace(refreshToken))
            {
                await tokenService.RevokeAsync(refreshToken, ct);
            }

            httpContext.Response.Cookies.Delete(jwtOptions.Value.RefreshCookieName, CreateRefreshCookieOptions(httpContext, jwtOptions.Value, DateTimeOffset.UtcNow.AddDays(-1)));
            return Results.Ok(new ApiResponse<object>(true, new { LoggedOut = true }));
        });

        group.MapGet("/me", async (
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            IProfileService profileService,
            CancellationToken ct) =>
        {
            if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            {
                return Results.Json(new ApiResponse<object>(false, Error: new ApiError("UNAUTHORIZED", "User is not authenticated.")), statusCode: StatusCodes.Status401Unauthorized);
            }

            var userId = httpContext.User.GetRequiredUserId();
            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Results.Json(new ApiResponse<object>(false, Error: new ApiError("USER_NOT_FOUND", "User does not exist.")), statusCode: StatusCodes.Status404NotFound);
            }

            var profile = await profileService.GetOrCreateProfileAsync(userId, user.Email, ct);
            return Results.Ok(new ApiResponse<AuthenticatedUserDto>(true, user.ToDto(profile)));
        }).RequireAuthorization();

        return app;
    }

    private static void WriteRefreshCookie(HttpContext httpContext, JwtOptions options, string refreshToken)
        => httpContext.Response.Cookies.Append(options.RefreshCookieName, refreshToken, CreateRefreshCookieOptions(httpContext, options, DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays)));

    private static CookieOptions CreateRefreshCookieOptions(HttpContext httpContext, JwtOptions options, DateTimeOffset expiresAt)
    {
        var secure = httpContext.Request.IsHttps;
        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Path = "/",
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            Secure = secure,
            Expires = expiresAt
        };
    }
}
