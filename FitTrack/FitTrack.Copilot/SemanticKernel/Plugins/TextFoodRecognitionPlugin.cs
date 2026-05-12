using FitTrack.Copilot.Models;
using FitTrack.Copilot.AI.Tooling;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace FitTrack.Copilot.AI.Plugins;

/// <summary>
/// Extracts structured food items from natural language meal descriptions.
/// </summary>
public sealed class TextFoodRecognitionPlugin
{
    private readonly IAIChatClientFactory _chatClientFactory;

    public TextFoodRecognitionPlugin(IAIChatClientFactory chatClientFactory)
    {
        _chatClientFactory = chatClientFactory;
    }

    public async Task<List<FoodItem>> RecognizeFoodFromTextAsync(string userId, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var chatClient = await _chatClientFactory.CreateAsync(userId, ct);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                You extract food items from user text.
                Return ONLY a JSON array.
                Each item must follow:
                {
                  "name": "food name",
                  "servingHint": "portion hint if available",
                  "confidence": 0.0-1.0
                }
                Use concise food names.
                """
            ),
            new(ChatRole.User, text)
        };

        var options = new ChatOptions
        {
            Temperature = 0.1f,
            MaxOutputTokens = 600,
            ResponseFormat = ChatResponseFormat.Json
        };

        var response = await chatClient.GetResponseAsync(messages, options, ct);
        var items = response.Text.Deserialize<List<FoodItem>>();
        return items?.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList() ?? [];
    }
}
