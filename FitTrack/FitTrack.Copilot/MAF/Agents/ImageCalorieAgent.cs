using System.ComponentModel;
using System.Text.RegularExpressions;
using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.Models;
using FitTrack.Copilot.AI.Orchestrator;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.MAF.Agents;

public sealed class ImageCalorieAgent
{
    private const string DefaultUserId = "default";

    private readonly FoodNutritionOrchestrator _foodNutritionOrchestrator;
    private readonly AIAgent _agent;

    public ImageCalorieAgent(
        IChatClient chatClient,
        FoodNutritionOrchestrator foodNutritionOrchestrator)
    {
        _foodNutritionOrchestrator = foodNutritionOrchestrator;

        _agent = chatClient.AsAIAgent(
            "image-calorie-agent",
            "An agent that estimates calories from food images.",
            """
            You are FitTrack's image calorie assistant.

            Your job is to estimate calories and macros from meal photos.

            Rules:
            - Always use the analyze_image_calories tool when an image is provided.
            - If the image is missing or invalid, explain that the caller must provide a valid image data URL.
            - Use the tool result as the source of truth for detected foods and calories.
            - Be concise and practical.
            - If confidence appears low or the detection is ambiguous, explicitly say the estimate may be rough.
            """,
            BuildTools());
    }

    [Description("Main entry point for calorie estimation from an image")]
    public async Task<string> HandleImageCalorieQueryAsync(
        string imageDataUrl,
        string? hint = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageDataUrl))
        {
            return "Image data is required. Provide a valid image data URL.";
        }

        var request = string.IsNullOrWhiteSpace(hint)
            ? $"Estimate calories from this meal image: {imageDataUrl}"
            : $"Estimate calories from this meal image: {imageDataUrl}\nHint: {hint}";

        var response = await _agent.RunAsync(
            request,
            cancellationToken: cancellationToken);

        return response.Text ?? "No response";
    }

    private IList<AITool> BuildTools()
    {
        return
        [
            AIFunctionFactory.Create(
                (Func<string, string?, CancellationToken, Task<string>>)AnalyzeImageCaloriesAsync,
                "analyze_image_calories",
                "Analyze a meal image from a data URL and return detected foods, calories, and macronutrients. Accepts imageDataUrl and an optional hint.")
        ];
    }

    private async Task<string> AnalyzeImageCaloriesAsync(string imageDataUrl, string? hint, CancellationToken ct)
    {
        var input = new VisionNutritionInput(
            Images: [FromDataUrl(imageDataUrl)],
            Hint: hint,
            UserId: DefaultUserId);

        var result = await _foodNutritionOrchestrator.ProcessVisionNutritionAsync(input, ct);
        return FormatNutritionResult(result);
    }

    private static FilePart FromDataUrl(string dataUrl)
    {
        var match = Regex.Match(
            dataUrl,
            @"^data:(?<ct>[^;]+);base64,(?<b64>.+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            throw new ArgumentException("Invalid data URL.", nameof(dataUrl));
        }

        var contentType = match.Groups["ct"].Value;
        var bytes = Convert.FromBase64String(match.Groups["b64"].Value);
        return new FilePart(bytes, contentType);
    }

    private static string FormatNutritionResult(NutritionResult result)
    {
        if (result.Items.Count == 0)
        {
            return string.IsNullOrWhiteSpace(result.Summary)
                ? "No food items were detected in the image."
                : result.Summary;
        }

        var output = string.IsNullOrWhiteSpace(result.Summary)
            ? $"Detected {result.Items.Count} food items totaling {result.TotalCalories:F0} kcal.\n\n"
            : $"{result.Summary}\n\n";

        foreach (var item in result.Items)
        {
            output += $"**{item.Name}**\n";
            output += $"Calories: {item.Calories:F0} kcal\n";
            output += $"Protein: {item.ProteinGrams:F1}g\n";
            output += $"Carbs: {item.CarbsGrams:F1}g\n";
            output += $"Fat: {item.FatGrams:F1}g\n";

            if (item.Confidence.HasValue)
            {
                output += $"Confidence: {item.Confidence.Value:P0}\n";
            }

            if (!string.IsNullOrWhiteSpace(item.ServingHint))
            {
                output += $"Serving: {item.ServingHint}\n";
            }

            output += "\n";
        }

        output += $"Total Calories: {result.TotalCalories:F0} kcal";
        return output;
    }
}
