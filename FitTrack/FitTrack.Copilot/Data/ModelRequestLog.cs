using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public enum ModelRequestType
{
    Chat,
    VisionRecognition,
    TextRecognition,
    NutritionAgent,
    WorkoutAgent
}

public enum ModelRequestStatus
{
    Succeeded,
    Failed
}

public class ModelRequestLog
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? ThreadId { get; set; }

    [MaxLength(64)]
    public string? ConversationMessageId { get; set; }

    [Required]
    public string ConnectorId { get; set; } = string.Empty;

    [Required]
    [MaxLength(160)]
    public string ConnectorDisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string ProviderPreset { get; set; } = string.Empty;

    public TenantModelProtocol Protocol { get; set; }

    [Required]
    [MaxLength(160)]
    public string ModelId { get; set; } = string.Empty;

    public ModelRequestType RequestType { get; set; }

    public ModelRequestStatus Status { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime CompletedAtUtc { get; set; }

    public int? DurationMs { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(128)]
    public string? ClientIpHash { get; set; }

    public long? InputTokens { get; set; }

    public long? OutputTokens { get; set; }

    public long? TotalTokens { get; set; }

    public long? CacheReadTokens { get; set; }

    public long? CacheWriteTokens { get; set; }

    public double? InputCostUsd { get; set; }

    public double? OutputCostUsd { get; set; }

    public double? CacheReadCostUsd { get; set; }

    public double? CacheWriteCostUsd { get; set; }

    public double? TotalCostUsd { get; set; }

    [MaxLength(128)]
    public string? ErrorCode { get; set; }

    [MaxLength(1024)]
    public string? ErrorMessage { get; set; }

    [MaxLength(1024)]
    public string? ToolEventsSummary { get; set; }

    [MaxLength(512)]
    public string? RequestSummary { get; set; }
}
