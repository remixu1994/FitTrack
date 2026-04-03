using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FitTrack.Copilot.Configuration;
using FitTrack.Copilot.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FitTrack.Copilot.Service;

public class AuthTokenService : IAuthTokenService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtOptions _jwtOptions;

    public AuthTokenService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken)> IssueAsync(ApplicationUser user, CancellationToken ct = default)
    {
        var accessToken = await CreateAccessTokenAsync(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        await _dbContext.SaveChangesAsync(ct);
        return (accessToken.Token, accessToken.ExpiresAtUtc, refreshToken);
    }

    public async Task<(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, ApplicationUser User)?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (storedToken is null || storedToken.RevokedAt is not null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        var replacementToken = GenerateRefreshToken();
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = HashToken(replacementToken);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = storedToken.ReplacedByTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });

        var accessToken = await CreateAccessTokenAsync(storedToken.User);
        await _dbContext.SaveChangesAsync(ct);
        return (accessToken.Token, accessToken.ExpiresAtUtc, replacementToken, storedToken.User);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private static string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
