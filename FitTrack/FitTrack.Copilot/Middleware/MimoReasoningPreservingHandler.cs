using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FitTrack.Copilot.Middleware;

/// <summary>
/// Ensures assistant messages with tool_calls retain their reasoning_content
/// across follow-up API calls. Required by Mimo (and similar OpenAI-compatible
/// providers) which return 400 if reasoning_content is missing from any
/// assistant message that contains tool_calls when thinking mode is active.
/// </summary>
public sealed class MimoReasoningPreservingHandler : DelegatingHandler
{
    private readonly ConcurrentDictionary<string, string> _reasoningByToolCallId = new();
    private readonly ILogger<MimoReasoningPreservingHandler>? _logger;

    public MimoReasoningPreservingHandler(ILogger<MimoReasoningPreservingHandler>? logger = null)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(requestBody))
            {
                LogRequestSummary(requestBody);

                var modified = InjectReasoningContent(requestBody);
                if (modified is not null)
                {
                    request.Content = new StringContent(modified, Encoding.UTF8, "application/json");
                }
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content is not null)
        {
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is "application/json" or "text/event-stream")
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                LogResponseSummary(responseBody, mediaType);
                ExtractReasoningContent(responseBody, mediaType);
                response.Content = new StringContent(responseBody, Encoding.UTF8, mediaType);
            }
        }

        return response;
    }

    private void LogRequestSummary(string requestBody)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Debug)) return;
        try
        {
            var root = JsonNode.Parse(requestBody);
            var model = root?["model"]?.GetValue<string>();
            var hasTools = root?["tools"] is JsonArray tools && tools.Count > 0;
            var toolCount = root?["tools"] is JsonArray t ? t.Count : 0;
            var messageCount = root?["messages"] is JsonArray m ? m.Count : 0;
            _logger.LogDebug("Mimo request: model={Model}, messages={MsgCount}, tools={ToolCount} (present={HasTools})",
                model, messageCount, toolCount, hasTools);
        }
        catch { /* ignore parse errors in logging */ }
    }

    private void LogResponseSummary(string responseBody, string mediaType)
    {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Debug)) return;
        try
        {
            if (mediaType == "text/event-stream")
            {
                var hasToolCalls = responseBody.Contains("\"tool_calls\"");
                var hasReasoning = responseBody.Contains("reasoning_content");
                _logger.LogDebug("Mimo SSE response: hasToolCalls={HasToolCalls}, hasReasoning={HasReasoning}, length={Length}",
                    hasToolCalls, hasReasoning, responseBody.Length);
            }
            else
            {
                var root = JsonNode.Parse(responseBody);
                var choices = root?["choices"] as JsonArray;
                var firstChoice = choices?.FirstOrDefault();
                var finishReason = firstChoice?["finish_reason"]?.GetValue<string>();
                var hasToolCalls = firstChoice?["message"]?["tool_calls"] is not null;
                var hasReasoning = firstChoice?["message"]?["reasoning_content"] is not null;
                _logger.LogDebug("Mimo JSON response: finishReason={FinishReason}, hasToolCalls={HasToolCalls}, hasReasoning={HasReasoning}",
                    finishReason, hasToolCalls, hasReasoning);
            }
        }
        catch { /* ignore parse errors in logging */ }
    }

    private string? InjectReasoningContent(string requestBody)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(requestBody);
        }
        catch (JsonException)
        {
            return null;
        }

        if (root?["messages"] is not JsonArray messages)
        {
            return null;
        }

        var modified = false;
        foreach (var node in messages)
        {
            if (node is not JsonObject msg)
            {
                continue;
            }

            if (msg["role"]?.GetValue<string>() != "assistant")
            {
                continue;
            }

            if (msg["tool_calls"] is not JsonArray toolCalls || toolCalls.Count == 0)
            {
                continue;
            }

            if (msg["reasoning_content"] is not null)
            {
                continue;
            }

            var toolCallIds = toolCalls
                .Select(tc => tc?["id"]?.GetValue<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Cast<string>()
                .ToList();

            var reasoningParts = toolCallIds
                .Select(id => _reasoningByToolCallId.TryGetValue(id, out var r) ? r : null)
                .Where(r => r is not null)
                .Distinct()
                .ToList();

            if (reasoningParts.Count == 1)
            {
                msg["reasoning_content"] = JsonValue.Create(reasoningParts[0]);
                modified = true;
            }
        }

        return modified ? root.ToJsonString() : null;
    }

    private void ExtractReasoningContent(string responseBody, string mediaType)
    {
        if (mediaType == "text/event-stream")
        {
            ExtractFromSse(responseBody);
            return;
        }

        ExtractFromJson(responseBody);
    }

    private void ExtractFromJson(string responseBody)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(responseBody);
        }
        catch (JsonException)
        {
            return;
        }

        if (root?["choices"] is not JsonArray choices)
        {
            return;
        }

        foreach (var choice in choices)
        {
            if (choice?["message"] is not JsonObject message)
            {
                continue;
            }

            StoreReasoningIfPresent(message);
        }
    }

    private void ExtractFromSse(string responseBody)
    {
        JsonObject? lastAssistantMessage = null;

        foreach (var line in responseBody.Split('\n'))
        {
            if (!line.StartsWith("data: "))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            JsonNode? chunk;
            try
            {
                chunk = JsonNode.Parse(data);
            }
            catch (JsonException)
            {
                continue;
            }

            if (chunk?["choices"] is not JsonArray choices)
            {
                continue;
            }

            foreach (var choice in choices)
            {
                if (choice?["delta"] is JsonObject delta)
                {
                    if (delta["role"]?.GetValue<string>() == "assistant")
                    {
                        lastAssistantMessage ??= new JsonObject();
                    }

                    if (lastAssistantMessage is not null)
                    {
                        if (delta["tool_calls"] is JsonArray toolCalls)
                        {
                            if (lastAssistantMessage["tool_calls"] is not JsonArray existing)
                            {
                                existing = new JsonArray();
                                lastAssistantMessage["tool_calls"] = existing;
                            }

                            foreach (var tc in toolCalls)
                            {
                                if (tc is JsonObject tcObj)
                                {
                                    MergeToolCallDelta(existing, tcObj);
                                }
                            }
                        }

                        var reasoning = delta["reasoning_content"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(reasoning))
                        {
                            var existing = lastAssistantMessage["reasoning_content"]?.GetValue<string>() ?? "";
                            lastAssistantMessage["reasoning_content"] = existing + reasoning;
                        }
                    }
                }

                if (choice?["finish_reason"]?.GetValue<string>() is "tool_calls" or "stop")
                {
                    if (lastAssistantMessage is not null)
                    {
                        StoreReasoningIfPresent(lastAssistantMessage);
                        lastAssistantMessage = null;
                    }
                }
            }
        }

        if (lastAssistantMessage is not null)
        {
            StoreReasoningIfPresent(lastAssistantMessage);
        }
    }

    private static void MergeToolCallDelta(JsonArray existing, JsonObject delta)
    {
        var index = delta["index"]?.GetValue<int>() ?? 0;

        while (existing.Count <= index)
        {
            existing.Add(new JsonObject());
        }

        if (existing[index] is not JsonObject target)
        {
            return;
        }

        if (delta["id"]?.GetValue<string>() is { Length: > 0 } id)
        {
            target["id"] = id;
        }

        if (delta["type"]?.GetValue<string>() is { Length: > 0 } type)
        {
            target["type"] = type;
        }

        if (delta["function"] is JsonObject funcDelta)
        {
            if (target["function"] is not JsonObject func)
            {
                func = new JsonObject();
                target["function"] = func;
            }

            if (funcDelta["name"]?.GetValue<string>() is { Length: > 0 } name)
            {
                func["name"] = name;
            }

            if (funcDelta["arguments"]?.GetValue<string>() is { Length: > 0 } args)
            {
                var existing2 = func["arguments"]?.GetValue<string>() ?? "";
                func["arguments"] = existing2 + args;
            }
        }
    }

    private void StoreReasoningIfPresent(JsonObject message)
    {
        var reasoning = message["reasoning_content"]?.GetValue<string>();
        if (string.IsNullOrEmpty(reasoning))
        {
            return;
        }

        if (message["tool_calls"] is not JsonArray toolCalls)
        {
            return;
        }

        foreach (var tc in toolCalls)
        {
            var id = tc?["id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id))
            {
                _reasoningByToolCallId[id] = reasoning;
            }
        }
    }
}
