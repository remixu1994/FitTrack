using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

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
//mimo-v2.5-pro，mimo-v2.5，mimo-v2.5-tts，mimo-v2.5-tts-voicedesign，mimo-v2.5-tts-voiceclone，mimo-v2-pro，mimo-v2-omni，mimo-v2-tts，mimo-v2-flash

var modelId = cli.ModelId ?? configurationRoot["Xiaomi:ModelId"] ?? "mimo-v2.5";
var apiKey = cli.ApiKey ?? GetRequired(configurationRoot, "Xiaomi:ApiKey");
var temperature = TryParseFloat(configurationRoot["Xiaomi:Temperature"], 0.2f);
var maxTokens = TryParseInt(configurationRoot["Xiaomi:MaxTokens"], 1024);
var prompt = cli.Prompt.Count > 0
    ? string.Join(' ', cli.Prompt)
    : "请用中文做一个简短自我介绍，并给我一条高蛋白早餐建议。";

Console.WriteLine("== Xiaomi Smoke Test ==");
Console.WriteLine($"Environment: {environmentName}");
Console.WriteLine($"Endpoint   : {endpoint}");
Console.WriteLine($"ModelId    : {modelId}");
Console.WriteLine($"Prompt     : {prompt}");
Console.WriteLine();

var chatClient = new OpenAIClient(
        new ApiKeyCredential(apiKey),
        new OpenAIClientOptions
        {
            Endpoint = new Uri(endpoint)
        })
    .GetChatClient(modelId)
    .AsIChatClient();

var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a concise assistant used for smoke testing Xiaomi MiMo from FitTrack.Copilot."),
    new(ChatRole.User, prompt)
};

var options = new ChatOptions
{
    ModelId = modelId,
    Temperature = temperature,
    MaxOutputTokens = maxTokens
};

try
{
    var response = await chatClient.GetResponseAsync(messages, options);

    Console.WriteLine("== Response ==");
    Console.WriteLine(response.Text);
    return;
}
catch (Exception ex)
{
    Console.Error.WriteLine("== Request Failed ==");
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine("Set one of these before running:");
    Console.Error.WriteLine("  Xiaomi__ApiKey");
    Console.Error.WriteLine("  XIAOMI_API_KEY");
    Environment.ExitCode = 1;
}

static void ApplyShorthandEnvironmentOverrides(IConfigurationRoot configuration)
{
    ApplyOverride(configuration, "Xiaomi:ApiKey", "XIAOMI_API_KEY");
    ApplyOverride(configuration, "Xiaomi:Endpoint", "XIAOMI_ENDPOINT");
    ApplyOverride(configuration, "Xiaomi:ModelId", "XIAOMI_MODEL_ID");
}

static void ApplyOverride(IConfigurationRoot configuration, string key, string envVarName)
{
    var value = Environment.GetEnvironmentVariable(envVarName);
    if (!string.IsNullOrWhiteSpace(value))
    {
        configuration[key] = value;
    }
}

static string GetRequired(IConfiguration configuration, string key)
{
    var value = configuration[key];
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new InvalidOperationException($"Missing required configuration value: {key}");
}

static int TryParseInt(string? value, int fallback)
    => int.TryParse(value, out var parsed) ? parsed : fallback;

static float TryParseFloat(string? value, float fallback)
    => float.TryParse(value, out var parsed) ? parsed : fallback;

static CommandLineArgs ParseArgs(string[] args)
{
    string? modelId = null;
    string? endpoint = null;
    string? apiKey = null;
    var prompt = new List<string>();

    foreach (var arg in args)
    {
        if (arg.StartsWith("--model=", StringComparison.OrdinalIgnoreCase))
        {
            modelId = arg["--model=".Length..];
            continue;
        }

        if (arg.StartsWith("--endpoint=", StringComparison.OrdinalIgnoreCase))
        {
            endpoint = arg["--endpoint=".Length..];
            continue;
        }

        if (arg.StartsWith("--api-key=", StringComparison.OrdinalIgnoreCase))
        {
            apiKey = arg["--api-key=".Length..];
            continue;
        }

        prompt.Add(arg);
    }

    return new CommandLineArgs(modelId, endpoint, apiKey, prompt);
}

internal sealed record CommandLineArgs(
    string? ModelId,
    string? Endpoint,
    string? ApiKey,
    IReadOnlyList<string> Prompt);
