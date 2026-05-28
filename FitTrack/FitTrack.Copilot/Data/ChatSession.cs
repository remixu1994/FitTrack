using System.ComponentModel.DataAnnotations;

namespace FitTrack.Copilot.Data;

public class ChatSession
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public List<ChatSessionMessage> Messages { get; set; } = new();
}

public class ChatSessionMessage
{
    public int Id { get; set; }
    
    public int ChatSessionId { get; set; }
    
    [Required]
    public string Role { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public ChatSession ChatSession { get; set; } = null!;
}