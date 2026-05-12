using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class TenantModelConnector
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string ProviderPreset { get; set; } = string.Empty;

    public TenantModelProtocol Protocol { get; set; }

    [Required]
    [MaxLength(512)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string ModelId { get; set; } = string.Empty;

    public string? EncryptedApiKey { get; set; }

    public bool IsDefault { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Tenant Tenant { get; set; } = null!;
}
