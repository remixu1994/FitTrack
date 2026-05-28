using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public interface IConversationService
{
    Task<IReadOnlyList<ConversationThread>> ListThreadsAsync(string userId, CancellationToken ct = default);
    Task<ConversationThread> CreateThreadAsync(string userId, string title, CancellationToken ct = default);
    Task<ConversationThread?> GetThreadAsync(string userId, string threadId, CancellationToken ct = default);
    Task DeleteThreadAsync(string userId, string threadId, CancellationToken ct = default);
    Task<ConversationMessage> CreateMessageAsync(string threadId, string role, string kind, string? contentText, object? contentJson, CancellationToken ct = default);
    Task<ConversationAttachment> CreateAttachmentAsync(string userId, string threadId, string messageId, string kind, string dataUrl, string? fileName, string? mimeType, long? fileSize, CancellationToken ct = default);
    Task<NutritionSnapshot?> CreateSnapshotAsync(string threadId, string messageId, AgentNutritionSnapshot? snapshot, CancellationToken ct = default);
    Task<(ConversationAttachment Attachment, Stream Stream)?> OpenAttachmentAsync(string userId, string attachmentId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(string threadId, CancellationToken ct = default);
    Task TouchThreadAsync(string threadId, CancellationToken ct = default);
}
