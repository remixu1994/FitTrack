using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace FitTrack.Copilot.Extension;

public static class ChatMessageExtensions
{
    public static ChatMessageContent ToChatMessageContent(this ChatMessage chatMessage)
    {
        var authorRole = chatMessage.Role.Value switch
        {
            "user" => AuthorRole.User,
            "assistant" => AuthorRole.Assistant,
            "system" => AuthorRole.System,
            _ => AuthorRole.User
        };
        
        return new ChatMessageContent(authorRole, chatMessage.Text);
    }
    
    public static ChatMessage ToChatMessage(this ChatMessageContent messageContent)
    {
        var chatRole = messageContent.Role.Label switch
        {
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            _ => ChatRole.User
        };
        
        return new ChatMessage(chatRole, messageContent.Content ?? string.Empty);
    }
}