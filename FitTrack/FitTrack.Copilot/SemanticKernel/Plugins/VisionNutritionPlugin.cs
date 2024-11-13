using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.SemanticKernel.Kernel;
using FitTrack.Copilot.SemanticKernel.Tooling;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using OpenAI.Chat;


namespace FitTrack.Copilot.SemanticKernel.Plugins;

public sealed class VisionNutritionPlugin(
    Microsoft.SemanticKernel.Kernel kernel,
    PromptLoader prompts)
    : IPlugin
{
    private readonly IChatCompletionService _chat = kernel.Services.GetRequiredService<IChatCompletionService>();

    public FunctionDescriptor Describe() => new(
        Name: "vision.nutrition.estimate",
        Summary: "Estimate macros & calories from a food image.",
        Inputs: new[] { "image/*", "hint?" },
        Outputs: "NutritionResult(JSON)",
        SafetyNotes: "No PII retention; best-effort estimates; include confidence.");

    public async Task<object?> InvokeAsync(FunctionContext ctx, CancellationToken ct = default)
        => await EstimateFromImageAsync(ctx, ct);

    public async Task<NutritionResult> EstimateFromImageAsync(FunctionContext ctx, CancellationToken ct)
    {
        // 1.载入系统提示词
        var system = await prompts.LoadAsync("vision_nutrition.system.md", ct);
        var chat = new ChatHistory();
        chat.AddSystemMessage(system);
        
        // 2.用户提示词
        if (ctx.TryGet<string>("hint", out var hint) && !string.IsNullOrWhiteSpace(hint))
            chat.AddUserMessage(hint);

        // 3.创建多模态消息
        var (bytes, mime) = ctx.RequireFirstFile(); // helper: throws if none
        var parts = new ChatMessageContentItemCollection
        {
            new ImageContent(new BinaryData(bytes), mimeType: mime),
            new TextContent("Respond JSON only.")
        };
        chat.AddUserMessage(parts);

        // 4.构造执行参数
        var exec = new PromptExecutionSettings
        {
            ServiceId = PromptExecutionSettings.DefaultServiceId,
            ModelId = "gpt-4o-mini", // 可选
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = 0.2,
                ["top_p"] = 1.0,
                ["max_tokens"] = 800,
                ["response_format"] = "json_object"
            }
        };
        var response = await _chat.GetChatMessageContentAsync(
            chat,
            exec,
            kernel: null,
            ct);

        return response.Content.Deserialize<NutritionResult>() ?? new NutritionResult();
    }
}
