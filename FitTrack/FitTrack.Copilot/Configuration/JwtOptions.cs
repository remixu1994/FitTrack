namespace FitTrack.Copilot.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "FitTrack.Copilot";

    public string Audience { get; set; } = "FitTrack.React";

    public string SigningKey { get; set; } = "ChangeThisDevelopmentSigningKeyForFitTrack123!";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 14;

    public string RefreshCookieName { get; set; } = "fittrack_rt";
}

