using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.SemanticKernel.Tooling;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;

namespace FitTrack.Copilot.SemanticKernel.Plugins;

public sealed record VisionNutritionInput(
    IReadOnlyList<FilePart>? Images,
    string? Hint);

public sealed class VisionNutritionPlugin(IChatClient chatClient, PromptLoader prompts)
{
    public async Task<NutritionResult> EstimateFromImageAsync(VisionNutritionInput input, CancellationToken ct)
    {
        List<ChatMessage> messages = await AddChatHistory(input, ct);

        var options = new ChatOptions
        {
            ModelId = "gpt-4o-mini",
            Temperature = 0.2f,
            MaxOutputTokens = 800,
            ResponseFormat = ChatResponseFormat.Json
        };

        var response = await chatClient.GetResponseAsync(
            messages,
            options,
            ct);

        return response.Text.Deserialize<NutritionResult>()
               ?? new NutritionResult();
    }

    private async Task<List<ChatMessage>> AddChatHistory(VisionNutritionInput input, CancellationToken ct)
    {
        var system = await prompts.LoadAsync("vision_nutrition.system.md", ct);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, system),
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
                        new TextContent("Respond JSON only.")
                    });
            }));
        }
        return messages;
    }

}