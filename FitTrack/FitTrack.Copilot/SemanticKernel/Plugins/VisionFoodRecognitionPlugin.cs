using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.AI.Tooling;
using Microsoft.Extensions.AI;
using System.Text.Json;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace FitTrack.Copilot.AI.Plugins;

public sealed class VisionFoodRecognitionPlugin(IChatClient chatClient, PromptLoader prompts)
{
    /// <summary>
    /// Analyze food images and return a list of recognized food items
    /// </summary>
    public async Task<List<FoodItem>> RecognizeFoodFromImagesAsync(VisionNutritionInput input, CancellationToken ct)
    {
        var messages = await BuildVisionMessagesAsync(input, ct);

        var options = new ChatOptions
        {
            Temperature = 0.2f,
            MaxOutputTokens = 800,
            ResponseFormat = ChatResponseFormat.Json
        };

        var response = await chatClient.GetResponseAsync(messages, options, ct);
        var foodItems = ParseFoodItems(response.Text);
        
        return foodItems?.Where(item => !string.IsNullOrEmpty(item.Name)).ToList() ?? new List<FoodItem>();
    }

    private async Task<List<ChatMessage>> BuildVisionMessagesAsync(VisionNutritionInput input, CancellationToken ct)
    {
        var systemPrompt = await prompts.LoadAsync("vision_nutrition.system.md", ct);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
        };

        if (!string.IsNullOrWhiteSpace(input.Hint))
        {
            messages.Add(new ChatMessage(ChatRole.User, input.Hint));
        }

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

        // Case 1: direct JSON array
        var direct = text.Deserialize<List<FoodItem>>();
        if (direct is { Count: > 0 })
        {
            return direct;
        }

        // Case 2: wrapped object { "items": [...] }
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
        public List<FoodItem> Items { get; set; } = new();
    }
}
