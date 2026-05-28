using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Api.Usda.Models;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Copilot.Endpoints;

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFood(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usda/foods").WithTags("USDA");

        // 搜索食物（例如 POST /api/usda/foods/search）
        group.MapPost("/search", async (
                [FromBody] SearchRequest req,
                [FromServices] IUsdaClient client) => await client.SearchAsync(req.Query))
            .WithName("SearchFood")
            .Produces<FoodItem>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        // 获取食物详情（例如 GET /api/usda/foods/12345）
        group.MapGet("/{foodId}", async (
                long foodId,
                [FromServices] IUsdaClient client) => await client.GetFoodAsync(foodId))
            .WithName("GetFoodDetail")
            .Produces<FoodDetail>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status502BadGateway);

        // AI 卡路里分析接口（支持文字、图片或混合输入）
        group.MapPost("/analyze", async (
                [FromBody] FoodRequest req,
                HttpContext httpContext,
                [FromServices] IFoodAiService ai,
                [FromServices] IModelRequestContextAccessor requestContextAccessor,
                CancellationToken ct) =>
            {
                using var _ = requestContextAccessor.BeginScope(context =>
                {
                    context.UserId = req.UserId ?? "anonymous";
                    context.RequestType = Data.ModelRequestType.Chat;
                    context.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
                    context.ClientIpHash = ModelRequestContext.HashClientIp(httpContext.Connection.RemoteIpAddress?.ToString());
                    context.RequestSummary = BuildAnalyzeSummary(req.Text, req.ImageDataUrl);
                });

                var result = await ai.AnalyzeAsync(req, ct);
                return Results.Ok(result);
            })
            .WithName("AnalyzeFoodCalories")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return app;
    }

    private static string BuildAnalyzeSummary(string? text, string? imageDataUrl)
    {
        if (!string.IsNullOrWhiteSpace(imageDataUrl) && !string.IsNullOrWhiteSpace(text))
        {
            return $"food analyze mixed: {Truncate(text)}";
        }

        if (!string.IsNullOrWhiteSpace(imageDataUrl))
        {
            return "food analyze image";
        }

        return $"food analyze text: {Truncate(text)}";
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().Replace("\r", " ").Replace("\n", " ");
        return normalized.Length <= 160 ? normalized : normalized[..160];
    }
}
