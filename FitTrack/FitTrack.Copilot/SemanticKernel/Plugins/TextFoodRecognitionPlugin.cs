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
    private readonly IModelRequestContextAccessor _requestContextAccessor;

    public TextFoodRecognitionPlugin(IAIChatClientFactory chatClientFactory, IModelRequestContextAccessor requestContextAccessor)
    {
        _chatClientFactory = chatClientFactory;
        _requestContextAccessor = requestContextAccessor;
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

        using var _ = _requestContextAccessor.BeginScope(context =>
        {
            context.UserId = userId;
            context.RequestType = Data.ModelRequestType.TextRecognition;
            context.RequestSummary = BuildSummary(text, "nutrition text recognition");
            context.ToolEvents = ["plugin:text-food-recognition"];
        });

        var response = await chatClient.GetResponseAsync(messages, options, ct);
        var items = response.Text.Deserialize<List<FoodItem>>();
        return items?.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList() ?? [];
    }

    private static string BuildSummary(string? text, string fallback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return fallback;
        }

        var normalized = text.Trim().Replace("\r", " ").Replace("\n", " ");
        return normalized.Length <= 200 ? normalized : normalized[..200];
    }
}
