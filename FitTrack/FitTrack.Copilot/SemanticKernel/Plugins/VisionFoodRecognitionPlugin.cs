using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.AI.Tooling;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace FitTrack.Copilot.AI.Plugins;

public sealed class VisionFoodRecognitionPlugin
{
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly PromptLoader _prompts;
    private readonly IModelRequestContextAccessor _requestContextAccessor;

    public VisionFoodRecognitionPlugin(IAIChatClientFactory chatClientFactory, PromptLoader prompts, IModelRequestContextAccessor requestContextAccessor)
    {
        _chatClientFactory = chatClientFactory;
        _prompts = prompts;
        _requestContextAccessor = requestContextAccessor;
    }

    /// <summary>
    /// Analyze food images and return a list of recognized food items
    /// </summary>
    public async Task<List<FoodItem>> RecognizeFoodFromImagesAsync(VisionNutritionInput input, CancellationToken ct)
    {
        var userId = string.IsNullOrWhiteSpace(input.UserId) ? "anonymous" : input.UserId;
        var chatClient = await _chatClientFactory.CreateAsync(userId, ct);
        var messages = await BuildVisionMessagesAsync(input, ct);

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            MaxOutputTokens = 800,
            ResponseFormat = ChatResponseFormat.Json
        };

        using var _ = _requestContextAccessor.BeginScope(context =>
        {
            context.UserId = userId;
            context.RequestType = Data.ModelRequestType.VisionRecognition;
            context.RequestSummary = BuildSummary(input.Hint, "vision meal photo");
            context.ToolEvents = ["plugin:vision-food-recognition"];
        });

        var response = await chatClient.GetResponseAsync(messages, options, ct);
        var foodItems = ParseFoodItems(response.Text);

        return foodItems?.Where(item => !string.IsNullOrEmpty(item.Name)).ToList() ?? [];
    }

    private async Task<List<ChatMessage>> BuildVisionMessagesAsync(VisionNutritionInput input, CancellationToken ct)
    {
        var systemPrompt = await _prompts.LoadAsync("vision_nutrition.system.md", ct);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
        };

        if (!string.IsNullOrWhiteSpace(input.Hint))
        {
            messages.Add(new ChatMessage(ChatRole.User, input.Hint));
        }

        messages.Add(new ChatMessage(ChatRole.System, AppLanguageSupport.BuildReplyInstruction(input.LanguageCode)));

        if (input.Images != null && input.Images.Any())
        {
            messages.AddRange(input.Images.Select(img =>
            {
                var base64 = Convert.ToBase64String(img.Bytes);
                var dataUrl = $"data:{img.ContentType};base64,{base64}";
                return new ChatMessage(ChatRole.User,
                    new List<AIContent>
                    {
                        new UriContent(new Uri(dataUrl), img.ContentType),
                        new TextContent("Respond with a JSON array of food items identified in the image.")
                    });
            }));
        }

        return messages;
    }

    private static List<FoodItem>? ParseFoodItems(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var direct = text.Deserialize<List<FoodItem>>();
        if (direct is { Count: > 0 })
        {
            return direct;
        }

        try
        {
            var wrapped = JsonSerializer.Deserialize<FoodItemsWrapper>(text, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return wrapped?.Items;
        }
        catch
        {
            return null;
        }
    }

    private sealed class FoodItemsWrapper
    {
        public List<FoodItem> Items { get; set; } = [];
    }

    private static string BuildSummary(string? hint, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return fallback;
        }

        var normalized = hint.Trim().Replace("\r", " ").Replace("\n", " ");
        return normalized.Length <= 200 ? normalized : normalized[..200];
    }
}
