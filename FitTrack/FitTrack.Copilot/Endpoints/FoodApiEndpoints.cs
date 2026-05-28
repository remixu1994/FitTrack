using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class FoodApiEndpoints
{
    public static IEndpointRouteBuilder MapFoodApiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Food").RequireAuthorization();

        group.MapGet("/foods/search", async (string query, Api.Usda.IUsdaClient client) =>
        {
            var item = await client.SearchAsync(query);
            return Results.Ok(new ApiResponse<object?>(true, item));
        })
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/food-records", async (HttpContext httpContext, IFoodRecordService foodRecordService, CancellationToken ct) =>
        {
            var records = await foodRecordService.GetFoodRecordsByUserIdAsync(httpContext.User.GetRequiredUserId(), ct);
            var data = records.Select(r => new FoodRecordDto(r.Id, r.FoodName, r.Calories, r.Protein, r.Carbs, r.Fat, r.ServingSize, r.ServingUnit, r.ConsumptionDate, r.MealType)).ToList();
            return Results.Ok(new ApiResponse<IReadOnlyList<FoodRecordDto>>(true, data));
        })
        .Produces<ApiResponse<IReadOnlyList<FoodRecordDto>>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/food-records", async (HttpContext httpContext, CreateFoodRecordRequest request, IFoodRecordService foodRecordService, CancellationToken ct) =>
        {
            var record = await foodRecordService.CreateFoodRecordAsync(new Data.FoodRecord
            {
                UserId = httpContext.User.GetRequiredUserId(),
                FoodName = request.FoodName,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbs = request.Carbs,
                Fat = request.Fat,
                ServingSize = request.ServingSize,
                ServingUnit = request.ServingUnit,
                ConsumptionDate = request.ConsumptionDate ?? DateTime.UtcNow,
                MealType = request.MealType
            }, ct);

            var dto = new FoodRecordDto(record.Id, record.FoodName, record.Calories, record.Protein, record.Carbs, record.Fat, record.ServingSize, record.ServingUnit, record.ConsumptionDate, record.MealType);
            return Results.Created($"/api/food-records/{record.Id}", new ApiResponse<FoodRecordDto>(true, dto));
        })
        .Produces<ApiResponse<FoodRecordDto>>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/foods/analyze", async (HttpContext httpContext, AnalyzeFoodRequest request, IFoodAiService foodAiService, IModelRequestContextAccessor requestContextAccessor, CancellationToken ct) =>
        {
            using var _ = requestContextAccessor.BeginScope(context =>
            {
                context.UserId = httpContext.User.GetRequiredUserId();
                context.RequestType = Data.ModelRequestType.Chat;
                context.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
                context.ClientIpHash = ModelRequestContext.HashClientIp(httpContext.Connection.RemoteIpAddress?.ToString());
                context.RequestSummary = BuildAnalyzeSummary(request.Text, request.ImageDataUrl);
            });

            var result = await foodAiService.AnalyzeAsync(new FoodRequest
            {
                Text = request.Text,
                ImageDataUrl = request.ImageDataUrl,
                UserId = httpContext.User.GetRequiredUserId()
            }, ct);

            return Results.Ok(new ApiResponse<object>(true, result));
        })
        .Produces<ApiResponse<object>>(StatusCodes.Status200OK)
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
