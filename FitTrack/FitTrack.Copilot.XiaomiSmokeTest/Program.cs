using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var cli = ParseArgs(args);
var environmentName =
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
    ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    ?? "Development";

var configurationRoot = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.shared.json", optional: true)
    .AddJsonFile($"appsettings.shared.{environmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

ApplyShorthandEnvironmentOverrides(configurationRoot);

var endpoint = cli.Endpoint ?? GetRequired(configurationRoot, "Xiaomi:Endpoint");
var modelId = cli.ModelId ?? configurationRoot["Xiaomi:ModelId"] ?? "mimo-v2.5";
var apiKey = cli.ApiKey ?? GetRequired(configurationRoot, "Xiaomi:ApiKey");
var temperature = TryParseFloat(configurationRoot["Xiaomi:Temperature"], 0.2f);
var maxTokens = TryParseInt(configurationRoot["Xiaomi:MaxTokens"], 1024);

Console.WriteLine("== Xiaomi Smoke Test (Microsoft.Agents.AI) ==");
Console.WriteLine($"Environment: {environmentName}");
Console.WriteLine($"Endpoint   : {endpoint}");
Console.WriteLine($"ModelId    : {modelId}");
Console.WriteLine();

// ════════════════════════════════════════════════════════════════════════════
// Step 1: WITHOUT reasoning handler — should reproduce the 400 bug
// ════════════════════════════════════════════════════════════════════════════
Console.WriteLine("━━━ Step 1: AIAgent multi-turn (no reasoning handler) ━━━");
Console.WriteLine("Expected: Turn 1 tool call works, Turn 2 returns 400 if reasoning_content is required.");
Console.WriteLine();

var chatClientRaw = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
    .GetChatClient(modelId)
    .AsIChatClient();

await RunAgentMultiTurnTest(chatClientRaw, "raw (no handler)");

// ════════════════════════════════════════════════════════════════════════════
// Step 2: WITH reasoning handler — should succeed
// ════════════════════════════════════════════════════════════════════════════
Console.WriteLine();
Console.WriteLine("━━━ Step 2: AIAgent multi-turn (with reasoning handler) ━━━");
Console.WriteLine("Expected: Both turns succeed, reasoning_content preserved across tool calls.");
Console.WriteLine();

var handler = new ReasoningPreservingHandler { InnerHandler = new HttpClientHandler() };
var chatClientFixed = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint),
            Transport = new HttpClientPipelineTransport(new HttpClient(handler))
        })
    .GetChatClient(modelId)
    .AsIChatClient();

await RunAgentMultiTurnTest(chatClientFixed, "fixed (with handler)");

Console.WriteLine();
Console.WriteLine("━━━ Test Complete ━━━");

// ════════════════════════════════════════════════════════════════════════════
// Multi-turn test using AIAgent.RunStreamingAsync (mirrors NutritionAgent)
// ════════════════════════════════════════════════════════════════════════════

async Task RunAgentMultiTurnTest(IChatClient chatClient, string label)
{
    // Build agent exactly like NutritionAgent does
    var agent = chatClient.AsAIAgent(
        "nutrition-test-agent",
        "Test agent for reasoning_content verification",
        """
        你是一个营养分析助手。当用户描述食物时，你必须调用 analyze_meal 工具来分析营养成分。
        不要自行估算，一定要使用工具。
        """,
        BuildTools());

    // ── Turn 1 ──
    Console.WriteLine($"[{label}] Turn 1: sending meal description (should trigger analyze_meal tool)...");

    var turn1Text = new StringBuilder();
    try
    {
        await foreach (var update in agent.RunStreamingAsync(
            new[] { new ChatMessage(ChatRole.User, "我今天中午吃了一碗牛肉面，大约300克。") }))
        {
            if (!string.IsNullOrWhiteSpace(update.Text))
            {
                turn1Text.Append(update.Text);
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[{label}] Turn 1 FAILED: {ex.Message}");
        return;
    }

    Console.WriteLine($"[{label}] Turn 1 response: {turn1Text.ToString()[..Math.Min(200, turn1Text.Length)]}...");
    Console.WriteLine();

    // ── Turn 2 ──
    Console.WriteLine($"[{label}] Turn 2: follow-up question (triggers re-send of history with tool_calls)...");

    var turn2Text = new StringBuilder();
    try
    {
        await foreach (var update in agent.RunStreamingAsync(
            new[] { new ChatMessage(ChatRole.User, "再来一个苹果大约多少卡？") }))
        {
            if (!string.IsNullOrWhiteSpace(update.Text))
            {
                turn2Text.Append(update.Text);
            }
        }

        Console.WriteLine($"[{label}] Turn 2 response: {turn2Text.ToString()[..Math.Min(200, turn2Text.Length)]}...");
        Console.WriteLine($"[{label}] ✅ Both turns succeeded.");
    }
    catch (Exception ex)
    {
        var is400 = ex.Message.Contains("400") || ex.Message.Contains("BadRequest");
        Console.Error.WriteLine($"[{label}] Turn 2 FAILED: {ex.Message}");
        if (is400)
        {
            Console.Error.WriteLine($"[{label}] ❌ 400 confirms: reasoning_content missing from assistant tool_calls.");
        }
    }
}

IList<AITool> BuildTools()
{
    return
    [
        AIFunctionFactory.Create(
            (Func<string, string>)((string meal) =>
            {
                Console.WriteLine($"  [Tool] analyze_meal(\"{meal}\") executed");
                return $"{{\"calories\": 520, \"protein_g\": 28, \"carbs_g\": 65, \"fat_g\": 14, \"note\": \"牛肉面约300g，含牛肉60g\"}}";
            }),
            "analyze_meal",
            "分析食物的营养成分，返回热量、蛋白质、碳水、脂肪等信息。"),
        AIFunctionFactory.Create(
            (Func<string, string>)((string food) =>
            {
                Console.WriteLine($"  [Tool] search_usda_food(\"{food}\") executed");
                return $"{{\"food\": \"{food}\", \"calories_per_100g\": 52, \"protein_g\": 0.3, \"carbs_g\": 14, \"fat_g\": 0.2}}";
            }),
            "search_usda_food",
            "Search USDA for a specific food and return nutrition info.")
    ];
}

// ════════════════════════════════════════════════════════════════════════════
// CLI / config helpers
// ════════════════════════════════════════════════════════════════════════════

static void ApplyShorthandEnvironmentOverrides(IConfigurationRoot configuration)
{
    ApplyOverride(configuration, "Xiaomi:ApiKey", "XIAOMI_API_KEY");
    ApplyOverride(configuration, "Xiaomi:Endpoint", "XIAOMI_ENDPOINT");
    ApplyOverride(configuration, "Xiaomi:ModelId", "XIAOMI_MODEL_ID");
}

static void ApplyOverride(IConfigurationRoot configuration, string key, string envVarName)
{
    var value = Environment.GetEnvironmentVariable(envVarName);
    if (!string.IsNullOrWhiteSpace(value)) configuration[key] = value;
}

static string GetRequired(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (!string.IsNullOrWhiteSpace(value)) return value;
    throw new InvalidOperationException($"Missing required configuration value: {key}");
}

static int TryParseInt(string? value, int fallback) => int.TryParse(value, out var parsed) ? parsed : fallback;
static float TryParseFloat(string? value, float fallback) => float.TryParse(value, out var parsed) ? parsed : fallback;

static CommandLineArgs ParseArgs(string[] args)
{
    string? modelId = null, endpoint = null, apiKey = null;
    var prompt = new List<string>();
    foreach (var arg in args)
    {
        if (arg.StartsWith("--model=", StringComparison.OrdinalIgnoreCase)) { modelId = arg["--model=".Length..]; continue; }
        if (arg.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase)) { endpoint = arg["--endpoint=".Length..]; continue; }
        if (arg.StartsWith("--api-key=", StringComparison.OrdinalIgnoreCase)) { apiKey = arg["--api-key=".Length..]; continue; }
        prompt.Add(arg);
    }
    return new CommandLineArgs(modelId, endpoint, apiKey, prompt);
}

internal sealed record CommandLineArgs(string? ModelId, string? Endpoint, string? ApiKey, IReadOnlyList<string> Prompt);

// ════════════════════════════════════════════════════════════════════════════
// Reasoning-preserving HTTP handler (mirrors MimoReasoningPreservingHandler)
// ════════════════════════════════════════════════════════════════════════════

sealed class ReasoningPreservingHandler : DelegatingHandler
{
    private readonly ConcurrentDictionary<string, string> _reasoningByToolCallId = new();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(body))
            {
                LogRequest(body);
                var modified = InjectReasoning(body);
                if (modified is not null)
                    request.Content = new StringContent(modified, Encoding.UTF8, "application/json");
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.Content is not null)
        {
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is "application/json" or "text/event-stream")
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                LogResponse(responseBody, mediaType);
                ExtractReasoning(responseBody, mediaType);
                response.Content = new StringContent(responseBody, Encoding.UTF8, mediaType);
            }
        }

        return response;
    }

    private void LogRequest(string body)
    {
        try
        {
            var root = JsonNode.Parse(body);
            var msgCount = root?["messages"] is JsonArray m ? m.Count : 0;
            var toolCount = root?["tools"] is JsonArray t ? t.Count : 0;
            var withTc = 0;
            var withRc = 0;
            if (root?["messages"] is JsonArray msgs)
                foreach (var msg in msgs)
                    if (msg is JsonObject o && o["role"]?.GetValue<string>() == "assistant" && o["tool_calls"] is JsonArray tc && tc.Count > 0)
                    { withTc++; if (o["reasoning_content"] is not null) withRc++; }
            Console.WriteLine($"  [HTTP] → msgs={msgCount}, tools={toolCount}, assistantWithToolCalls={withTc}, withReasoning={withRc}");
        }
        catch { }
    }

    private void LogResponse(string body, string mediaType)
    {
        try
        {
            if (mediaType == "text/event-stream")
                Console.WriteLine($"  [HTTP] ← SSE: hasToolCalls={body.Contains("\"tool_calls\"")}, hasReasoning={body.Contains("reasoning_content")}, len={body.Length}");
            else
            {
                var first = (JsonNode.Parse(body)?["choices"] as JsonArray)?.FirstOrDefault();
                Console.WriteLine($"  [HTTP] ← JSON: finish={first?["finish_reason"]}, hasToolCalls={first?["message"]?["tool_calls"] is not null}, hasReasoning={first?["message"]?["reasoning_content"] is not null}");
            }
        }
        catch { }
    }

    private string? InjectReasoning(string body)
    {
        var root = JsonNode.Parse(body);
        if (root?["messages"] is not JsonArray messages) return null;
        var modified = false;
        foreach (var node in messages)
        {
            if (node is not JsonObject msg) continue;
            if (msg["role"]?.GetValue<string>() != "assistant") continue;
            if (msg["tool_calls"] is not JsonArray tc || tc.Count == 0) continue;
            if (msg["reasoning_content"] is not null) continue;
            var ids = tc.Select(t => t?["id"]?.GetValue<string>()).Where(id => !string.IsNullOrEmpty(id)).Cast<string>().ToList();
            var parts = ids.Select(id => _reasoningByToolCallId.TryGetValue(id, out var r) ? r : null).Where(r => r is not null).Distinct().ToList();
            if (parts.Count == 1)
            {
                msg["reasoning_content"] = JsonValue.Create(parts[0]);
                modified = true;
                Console.WriteLine($"  [Handler] Injected reasoning_content for tool_calls: [{string.Join(", ", ids)}]");
            }
        }
        return modified ? root.ToJsonString() : null;
    }

    private void ExtractReasoning(string body, string mediaType)
    {
        if (mediaType == "text/event-stream") { ExtractFromSse(body); return; }
        var root = JsonNode.Parse(body);
        if (root?["choices"] is not JsonArray choices) return;
        foreach (var choice in choices)
            if (choice?["message"] is JsonObject msg) StoreIfPresent(msg);
    }

    private void ExtractFromSse(string body)
    {
        JsonObject? current = null;
        foreach (var line in body.Split('\n'))
        {
            if (!line.StartsWith("data: ")) continue;
            var data = line["data:".Length..].Trim();
            if (data == "[DONE]") break;
            JsonNode? chunk;
            try { chunk = JsonNode.Parse(data); } catch { continue; }
            if (chunk?["choices"] is not JsonArray choices) continue;
            foreach (var choice in choices)
            {
                if (choice?["delta"] is JsonObject delta)
                {
                    if (delta["role"]?.GetValue<string>() == "assistant") current ??= new JsonObject();
                    if (current is not null)
                    {
                        if (delta["tool_calls"] is JsonArray tc)
                        {
                            if (current["tool_calls"] is not JsonArray existing) { existing = new JsonArray(); current["tool_calls"] = existing; }
                            foreach (var t in tc) if (t is JsonObject tcObj) MergeDelta(existing, tcObj);
                        }
                        var reasoning = delta["reasoning_content"]?.GetValue<string>();
                        if (!string.IsNullOrEmpty(reasoning))
                            current["reasoning_content"] = (current["reasoning_content"]?.GetValue<string>() ?? "") + reasoning;
                    }
                }
                if (choice?["finish_reason"]?.GetValue<string>() is "tool_calls" or "stop")
                    if (current is not null) { StoreIfPresent(current); current = null; }
            }
        }
        if (current is not null) StoreIfPresent(current);
    }

    private static void MergeDelta(JsonArray existing, JsonObject delta)
    {
        var index = delta["index"]?.GetValue<int>() ?? 0;
        while (existing.Count <= index) existing.Add(new JsonObject());
        if (existing[index] is not JsonObject target) return;
        if (delta["id"]?.GetValue<string>() is { Length: > 0 } id) target["id"] = id;
        if (delta["type"]?.GetValue<string>() is { Length: > 0 } type) target["type"] = type;
        if (delta["function"] is JsonObject fDelta)
        {
            if (target["function"] is not JsonObject f) { f = new JsonObject(); target["function"] = f; }
            if (fDelta["name"]?.GetValue<string>() is { Length: > 0 } name) f["name"] = name;
            if (fDelta["arguments"]?.GetValue<string>() is { Length: > 0 } args) f["arguments"] = (f["arguments"]?.GetValue<string>() ?? "") + args;
        }
    }

    private void StoreIfPresent(JsonObject msg)
    {
        var reasoning = msg["reasoning_content"]?.GetValue<string>();
        if (string.IsNullOrEmpty(reasoning) || msg["tool_calls"] is not JsonArray tc) return;
        foreach (var t in tc)
        {
            var id = t?["id"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(id))
            {
                _reasoningByToolCallId[id] = reasoning;
                Console.WriteLine($"  [Handler] Stored reasoning_content ({reasoning.Length} chars) for tool_call {id[..12]}...");
            }
        }
    }
}
