using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class UserProfile
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DisplayName { get; set; }

    [MaxLength(32)]
    public string? Sex { get; set; }

    public int? Age { get; set; }

    public double? HeightCm { get; set; }

    public double? WeightKg { get; set; }

    public double? BodyFatPercent { get; set; }

    [MaxLength(64)]
    public string? ActivityLevel { get; set; }

    [MaxLength(256)]
    public string? Goal { get; set; }

    [MaxLength(1024)]
    public string? Preferences { get; set; }

    public string? PreferredModelConnectorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}
