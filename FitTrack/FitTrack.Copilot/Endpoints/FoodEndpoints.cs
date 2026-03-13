using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Api.Usda.Models;
using FitTrack.Copilot.Service;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Copilot.Endpoints;

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFood(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/usda/food").WithTags("Copilot-Vision");

        // 查询食物营养（例如 /usda/food/search）
        group.MapPost("/search", async (
                [FromBody] SearchRequest req,
                [FromServices] IUsdaClient client) => await client.SearchAsync(req.Query))
            .WithName("SearchFood");

        // 查询指定食物的详细营养（例如 /usda/food/12345）
        group.MapGet("/{foodId}", async (
                long foodId,
                [FromServices] IUsdaClient client) => await client.GetFoodAsync(foodId))
            .WithName("GetFoodDetail");

        // 统一卡路里分析接口（支持文字、图片或混合输入）
        group.MapPost("/analyze", async (
                [FromBody] FoodRequest req,
                [FromServices] IFoodAiService ai,
                CancellationToken ct) =>
            {
                var result = await ai.AnalyzeAsync(req, ct);
                return Results.Ok(result);
            })
            .WithName("AnalyzeFoodCalories");

        return app;
    }
}
