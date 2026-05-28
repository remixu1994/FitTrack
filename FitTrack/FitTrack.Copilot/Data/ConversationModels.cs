using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class ConversationThread
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ArchivedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public List<ConversationMessage> Messages { get; set; } = new();

    public List<NutritionSnapshot> Snapshots { get; set; } = new();
}

public class ConversationMessage
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string ThreadId { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Kind { get; set; } = "text";

    public string? ContentText { get; set; }

    public string? ContentJson { get; set; }

    public int TurnIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConversationThread Thread { get; set; } = null!;

    public List<ConversationAttachment> Attachments { get; set; } = new();

    public NutritionSnapshot? Snapshot { get; set; }
}

public class ConversationAttachment
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string ThreadId { get; set; } = string.Empty;

    [Required]
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Kind { get; set; } = "meal_photo";

    [MaxLength(260)]
    public string? FileName { get; set; }

    [MaxLength(128)]
    public string? MimeType { get; set; }

    public long? FileSize { get; set; }

    [Required]
    [MaxLength(512)]
    public string StoragePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConversationThread Thread { get; set; } = null!;

    public ConversationMessage Message { get; set; } = null!;
}

public class NutritionSnapshot
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required]
    public string ThreadId { get; set; } = string.Empty;

    [Required]
    public string MessageId { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? TrainingType { get; set; }

    [MaxLength(32)]
    public string? DayType { get; set; }

    public int? TargetCalories { get; set; }

    public int? TargetProteinG { get; set; }

    public int? TargetCarbsG { get; set; }

    public int? TargetFatG { get; set; }

    public int? ConsumedCalories { get; set; }

    public double? ConsumedProteinG { get; set; }

    public double? ConsumedCarbsG { get; set; }

    public double? ConsumedFatG { get; set; }

    public int? RemainingCalories { get; set; }

    public double? RemainingProteinG { get; set; }

    public double? RemainingCarbsG { get; set; }

    public double? RemainingFatG { get; set; }

    public string? NextSuggestions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ConversationThread Thread { get; set; } = null!;

    public ConversationMessage Message { get; set; } = null!;
}
