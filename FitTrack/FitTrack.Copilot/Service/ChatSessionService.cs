using FitTrack.Copilot.Data;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public class ChatSessionService
{
    
}

public interface IChatSessionService
{
    Task<int> SaveChatSessionAsync(string userId, string title, IEnumerable<ChatMessage> messages);
    Task<ChatSession?> GetChatSessionAsync(int sessionId, string userId);
    Task<IEnumerable<ChatSession>> GetUserChatSessionsAsync(string userId);
    Task<bool> DeleteChatSessionAsync(int sessionId, string userId);
    Task UpdateChatSessionAsync(int sessionId, string userId, IEnumerable<ChatMessage> messages);
}