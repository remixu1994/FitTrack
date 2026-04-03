using System.Net.Http;
using System.Text.Json;
using FitTrack.Copilot.Agents;
using FitTrack.Copilot.Configuration;
using FitTrack.Copilot.Data;
using Microsoft.Extensions.Options;

namespace FitTrack.Copilot.Service;

public sealed class PythonCoachChatService : ICoachChatService
{
    private readonly IOptions<PythonAgentOptions> _options;
    private readonly IPythonCoachClient _pythonCoachClient;
    private readonly CoachSupervisorAgent _fallbackCoach;
    private readonly IConversationMemory _conversationMemory;
    private readonly ILogger<PythonCoachChatService> _logger;

    public PythonCoachChatService(
        IOptions<PythonAgentOptions> options,
        IPythonCoachClient pythonCoachClient,
        CoachSupervisorAgent fallbackCoach,
        IConversationMemory conversationMemory,
        ILogger<PythonCoachChatService> logger)
    {
        _options = options;
        _pythonCoachClient = pythonCoachClient;
        _fallbackCoach = fallbackCoach;
        _conversationMemory = conversationMemory;
        _logger = logger;
    }

    public async Task<AgentExecutionResult> SendAsync(string userId, string threadId, string? text, string? imageDataUrl, CancellationToken ct = default)
    {
        if (!_options.Value.Enabled)
        {
            return await _fallbackCoach.SendAsync(userId, threadId, text, imageDataUrl, ct);
        }

        try
        {
            var recentMessages = await _conversationMemory.GetRecentMessagesAsync(threadId, 8, ct);
            var request = new PythonChatRequest(
                userId,
                threadId,
                string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
                imageDataUrl,
                recentMessages.Select(MapMessage).ToList(),
                DateTime.UtcNow);

            var response = await _pythonCoachClient.SendAsync(request, ct);
            var toolEvents = (response.ToolEvents ?? Array.Empty<string>()).ToList();
            if (!string.IsNullOrWhiteSpace(response.TraceId))
            {
                toolEvents.Add($"python.trace:{response.TraceId}");
            }

            return new AgentExecutionResult(
                response.AgentName,
                string.IsNullOrWhiteSpace(response.Message) ? "No response." : response.Message,
                response.StructuredPayload,
                response.Snapshot,
                toolEvents);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            _logger.LogWarning(ex, "Python agent chat failed for thread {ThreadId}. Falling back to in-process coach.", threadId);
            return await _fallbackCoach.SendAsync(userId, threadId, text, imageDataUrl, ct);
        }
    }

    private static PythonRecentMessage MapMessage(ConversationMessage message)
        => new(
            message.Role,
            message.Kind,
            message.ContentText,
            DeserializeJson(message.ContentJson));

    private static object? DeserializeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<object>(json);
        }
        catch
        {
            return json;
        }
    }
}
