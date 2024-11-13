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

            // Delegate to SK-backed plugin
            var result = await _vision.EstimateFromImageAsync(ctx, ct);

            // Optional: basic sanity check
            if (result is null || result.Items.Count == 0)
                return new AgentResult(true, new NutritionResult(), "No food items confidently detected.");

            return new AgentResult(true, result, null);
        }
        catch (Exception ex)
        {
            return new AgentResult(false, null, $"Image analysis failed: {ex.Message}");
        }
    }
}