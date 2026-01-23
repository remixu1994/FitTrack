using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Abstractions.Agents;
using FitTrack.Copilot.Abstractions.Models;
using FitTrack.Copilot.SemanticKernel.Plugins;

namespace FitTrack.Copilot.Agent;

/// <summary>
/// Image-only nutrition agent. Handles: "vision.nutrition.estimate"
/// </summary>
public sealed class ImageNutritionAgent : IAgent
{
    public AgentDescriptor Descriptor { get; } = new(
        Name: "image-nutrition",
        Purpose: "Estimate macros/calories from a single food image.",
        Capabilities: new[] { "vision.nutrition.estimate" },
        Metadata: new Dictionary<string, string> { ["domain"] = "nutrition", ["modality"] = "vision" });

    private readonly VisionNutritionPlugin _vision;

    public ImageNutritionAgent(VisionNutritionPlugin vision)
    {
        _vision = vision;
    }

    public async Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken ct = default)
    {
        if (!Descriptor.Supports(request.Intent))
            return new AgentResult(false, null, $"Unsupported intent: {request.Intent}");

        try
        {
            // Build function context from agent request (files + inputs)
            var ctx = FunctionContext.FromAgentRequest(request);
            
            // Require first file and assert it is an image/*
            ctx.RequireFirstFile("image/");
            var hint = string.Empty;

            if (request.Inputs.TryGetValue("hint", out object? ht))
            {
                hint = ht?.ToString() ?? "分析图片的内容后，再分析统计 他的卡路里。";
            }
            
            // Delegate to SK-backed plugin
            var result = await _vision.EstimateFromImageAsync(new VisionFoodPlugin(request.Files, hint), ct);

            // Optional: basic sanity check
            return result.Items.Count == 0 ? new AgentResult(true, new NutritionResult(), "No food items confidently detected.") : new AgentResult(true, result, null);
        }
        catch (Exception ex)
        {
            return new AgentResult(false, null, $"Image analysis failed: {ex.Message}");
        }
    }
}