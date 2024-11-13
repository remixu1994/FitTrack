using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Api;

namespace FitTrack.Copilot.Endpoints;


public static class CopilotVisionEndpoints
{
    public static IEndpointRouteBuilder MapCopilotVision(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/copilot/vision").WithTags("Copilot-Vision");

        // POST /copilot/vision/estimate
        group.MapPost("/estimate", async (HttpRequest req, IEnumerable<IAgent> agents, CancellationToken ct) =>
            {
                // 找到支持 vision.nutrition.estimate 的 Agent
                var agent = agents.FirstOrDefault(a => a.Descriptor.Supports("vision.nutrition.estimate"))
                            ?? throw new InvalidOperationException("No agent for 'vision.nutrition.estimate'.");

                // 解析 multipart：files + inputs
                var (files, inputs) = await HttpMultipartHelper.ReadAsync(req, ct);
                if (files.Count == 0) return Results.BadRequest("image file required.");

                // 允许从表单传 serviceId / modelId / hint
                var request = new AgentRequest(
                    UserId: "me", // TODO: integrate with auth
                    Intent: "vision.nutrition.estimate",
                    Inputs: inputs,
                    Files: files, 
                    CorrelationId: req.HttpContext.TraceIdentifier
                );

                // 这里直接调用插件所在的 Agent（Agent 内部委派给 VisionNutritionPlugin）
                var result = await agent.ExecuteAsync(request, ct);

                return result.Success
                    ? Results.Ok(result.Data)
                    : Results.BadRequest(result.Message);
            })
            .DisableAntiforgery() // 如果前端是单页应用，常见需要关闭 CSRF
            .Accepts<IFormFile>("multipart/form-data")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}