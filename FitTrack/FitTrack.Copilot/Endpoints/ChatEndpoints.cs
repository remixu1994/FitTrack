using System.Text.Json;
using FitTrack.Copilot.Api;
using FitTrack.Copilot.Api.Contracts;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Endpoints;

public static class ChatEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat").WithTags("Chat").RequireAuthorization();

        group.MapGet("/threads", async (HttpContext httpContext, IConversationService conversationService, CancellationToken ct) =>
        {
            var data = await conversationService.ListThreadsAsync(httpContext.User.GetRequiredUserId(), ct);
            return Results.Ok(new ApiResponse<IReadOnlyList<ConversationThreadDto>>(true, data.Select(Mappers.ToDto).ToList()));
        });

        group.MapPost("/threads", async (HttpContext httpContext, CreateThreadRequest request, IConversationService conversationService, CancellationToken ct) =>
        {
            var thread = await conversationService.CreateThreadAsync(httpContext.User.GetRequiredUserId(), request.Title, ct);
            return Results.Created($"/api/chat/threads/{thread.Id}", new ApiResponse<ConversationThreadDto>(true, thread.ToDto()));
        });

        group.MapGet("/threads/{threadId}", async (HttpContext httpContext, string threadId, IConversationService conversationService, CancellationToken ct) =>
        {
            var thread = await conversationService.GetThreadAsync(httpContext.User.GetRequiredUserId(), threadId, ct);
            return thread is null
                ? Results.NotFound(new ApiResponse<object>(false, Error: new ApiError("THREAD_NOT_FOUND", "Thread not found.")))
                : Results.Ok(new ApiResponse<ThreadDetailDto>(true, thread.ToDetailDto()));
        });

        group.MapDelete("/threads/{threadId}", async (HttpContext httpContext, string threadId, IConversationService conversationService, CancellationToken ct) =>
        {
            await conversationService.DeleteThreadAsync(httpContext.User.GetRequiredUserId(), threadId, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/attachments/{attachmentId}", async (HttpContext httpContext, string attachmentId, IConversationService conversationService, CancellationToken ct) =>
        {
            var result = await conversationService.OpenAttachmentAsync(httpContext.User.GetRequiredUserId(), attachmentId, ct);
            if (result is null)
            {
                return Results.NotFound(new ApiResponse<object>(false, Error: new ApiError("ATTACHMENT_NOT_FOUND", "Attachment not found.")));
            }

            return Results.File(result.Value.Stream, result.Value.Attachment.MimeType ?? "application/octet-stream", fileDownloadName: result.Value.Attachment.FileName);
        });

        group.MapPost("/messages", HandleMessageStreamAsync);

        return app;
    }

    private static async Task HandleMessageStreamAsync(
        HttpContext httpContext,
        SendMessageRequest request,
        IConversationService conversationService,
        ICoachChatService coachChatService,
        CancellationToken ct)
    {
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/x-ndjson; charset=utf-8";

        var userId = httpContext.User.GetRequiredUserId();
        var thread = await conversationService.GetThreadAsync(userId, request.ThreadId, ct);
        if (thread is null)
        {
            await WriteEventAsync(httpContext, new { type = "error", error = "Thread not found." }, ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ContentText) && request.MealPhoto is null)
        {
            await WriteEventAsync(httpContext, new { type = "error", error = "Message requires text or meal photo." }, ct);
            return;
        }

        var userText = string.IsNullOrWhiteSpace(request.ContentText) && request.MealPhoto is not null
            ? "Uploaded a meal photo for analysis."
            : request.ContentText;

        var userContent = request.ContentJson is null
            ? null
            : new Dictionary<string, object?>(request.ContentJson);

        var userKind = request.MealPhoto is null
            ? "text"
            : string.IsNullOrWhiteSpace(request.ContentText)
                ? "meal_photo"
                : "multimodal";

        var userMessage = await conversationService.CreateMessageAsync(request.ThreadId, "user", userKind, userText, userContent, ct);
        if (request.MealPhoto is not null)
        {
            var attachment = await conversationService.CreateAttachmentAsync(
                userId,
                request.ThreadId,
                userMessage.Id,
                "meal_photo",
                request.MealPhoto.DataUrl,
                request.MealPhoto.Name,
                request.MealPhoto.MimeType,
                request.MealPhoto.Size,
                ct);
            userMessage.Attachments.Add(attachment);
        }

        await WriteEventAsync(httpContext, new { type = "user_message", message = userMessage.ToDto() }, ct);

        try
        {
            AgentExecutionResult? agentResponse = null;
            await foreach (var update in coachChatService.SendStreamingAsync(
                               userId,
                               request.ThreadId,
                               request.ContentText,
                               request.MealPhoto?.DataUrl,
                               ct).WithCancellation(ct))
            {
                switch (update.Type)
                {
                    case CoachStreamEventType.ToolEvent when !string.IsNullOrWhiteSpace(update.Value):
                        await WriteEventAsync(httpContext, new { type = "tool_event", value = update.Value }, ct);
                        break;
                    case CoachStreamEventType.Token when !string.IsNullOrWhiteSpace(update.Value):
                        await WriteEventAsync(httpContext, new { type = "token", value = update.Value }, ct);
                        break;
                    case CoachStreamEventType.Completed when update.Result is not null:
                        agentResponse = update.Result;
                        break;
                }
            }

            if (agentResponse is null)
            {
                throw new InvalidOperationException("Coach response completed without a final result.");
            }

            var assistantMessage = await conversationService.CreateMessageAsync(
                request.ThreadId,
                "assistant",
                "text",
                agentResponse.Message,
                agentResponse.StructuredPayload,
                ct);

            await WriteEventAsync(httpContext, new { type = "assistant_message", message = assistantMessage.ToDto() }, ct);

            var snapshot = await conversationService.CreateSnapshotAsync(request.ThreadId, assistantMessage.Id, agentResponse.Snapshot, ct);
            if (snapshot is not null)
            {
                await WriteEventAsync(httpContext, new { type = "snapshot", snapshot = snapshot.ToDto() }, ct);
            }

            await WriteEventAsync(httpContext, new { type = "done" }, ct);
        }
        catch (Exception ex)
        {
            await WriteEventAsync(httpContext, new { type = "error", error = ex.Message }, ct);
        }
    }

    private static async Task WriteEventAsync(HttpContext context, object payload, CancellationToken ct)
    {
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions) + "\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
}
