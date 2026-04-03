using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class RefreshToken
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAt { get; set; }

    [MaxLength(128)]
    public string? ReplacedByTokenHash { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
