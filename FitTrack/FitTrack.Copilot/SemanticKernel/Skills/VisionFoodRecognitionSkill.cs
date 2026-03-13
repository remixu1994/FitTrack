using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.SemanticKernel.Tooling;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;
using TextContent = Microsoft.Extensions.AI.TextContent;

namespace FitTrack.Copilot.SemanticKernel.Skills;

public class VisionFoodRecognitionSkill
{
    private readonly IChatClient _chatClient;
    private readonly PromptLoader _prompts;

    public VisionFoodRecognitionSkill(IChatClient chatClient, PromptLoader prompts)
    {
        _chatClient = chatClient;
        _prompts = prompts;
    }

    [KernelFunction, Description("Analyze food images and return a list of recognized food items")]
    public async Task<List<FoodItem>> RecognizeFoodFromImagesAsync(
        [Description("Vision nutrition input with images and hint")] VisionNutritionInput input,
        CancellationToken ct)
    {
        var messages = await BuildVisionMessagesAsync(input, ct);

        var options = new ChatOptions
        {
            ModelId = "gpt-4o-mini",
            Temperature = 0.2f,
            MaxOutputTokens = 800,
            ResponseFormat = ChatResponseFormat.Json
        };

        var response = await _chatClient.GetResponseAsync(messages, options, ct);

        // Parse the response to extract food items
        var foodItems = response.Text.Deserialize<List<FoodItem>>();
        
        return foodItems?.Where(item => !string.IsNullOrEmpty(item.Name)).ToList() ?? new List<FoodItem>();
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
}
