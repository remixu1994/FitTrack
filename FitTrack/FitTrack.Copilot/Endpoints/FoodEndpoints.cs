using FitTrack.Copilot.Api.Usda;
using FitTrack.Copilot.Api.Usda.Models;
using Microsoft.AspNetCore.Mvc;

namespace FitTrack.Copilot.Endpoints;

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFood(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/usda/food").WithTags("Copilot-Vision");
        // 查询食物营养（例如 /api/food/search?name=banana）
        group.MapGet("/search", async (
                [FromBody] SearchRequest req,
                [FromServices]IUsdaClient client) => await client.SearchAsync(req.Query))
            .WithName("SearchFood");

        // 查询指定食物的详细营养（例如 /api/food/12345）
        group.MapGet("/{foodId}", async (
                long foodId,
                [FromServices]IUsdaClient client) => await client.GetFoodAsync(foodId))
            .WithName("GetFoodDetail");
        return app;
    }
}