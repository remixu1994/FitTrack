using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class Tenant
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Slug { get; set; } = string.Empty;

    public bool IsSystemDefault { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<ApplicationUser> Users { get; set; } = [];

    public List<TenantModelConnector> ModelConnectors { get; set; } = [];
}
