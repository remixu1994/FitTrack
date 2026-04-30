using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using System.ClientModel;
using FitTrack.Copilot.Agents;
using FitTrack.Copilot.Middleware;
using FitTrack.Copilot.Configuration;
using FitTrack.Copilot.SemanticKernel.Orchestrator;
using FitTrack.Copilot.SemanticKernel.Plugins;
using FitTrack.Copilot.SemanticKernel.RAG;
using FitTrack.Copilot.SemanticKernel.Tooling;
using FitTrack.Copilot.Service;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;

namespace FitTrack.Copilot.Extension;

public static class CopilotServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PromptOptions>(configuration.GetSection("Prompts"));
        services.Configure<PythonAgentOptions>(configuration.GetSection(PythonAgentOptions.SectionName));
        services.AddSingleton<PromptLoader>();
        services.AddChatClient(configuration);
        services.AddHttpClient<IPythonCoachClient, PythonCoachClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PythonAgentOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));
        });

        // Register Semantic Kernel plugins
        services.AddTransient<VisionFoodRecognitionPlugin>();
        services.AddTransient<TextFoodRecognitionPlugin>();
        services.AddTransient<NutritionPlugin>();
        services.AddTransient<WorkoutPlugin>();
        services.AddTransient<HealthDataPlugin>();
        
        // Register RAG service
        services.AddTransient<FitnessRAGService>();
        
        // Register orchestrators
        services.AddTransient<FoodNutritionOrchestrator>();

        // Register application services
        services.AddScoped<IFoodAiService, FoodAiService>();
        services.AddScoped<IFoodRecordService, FoodRecordService>();
        services.AddScoped<IFitnessService, FitnessService>();
        services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationMemory, ConversationMemory>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        // Register tool services
        services.AddScoped<INutritionTools, NutritionTools>();
        services.AddScoped<IWorkoutTools, WorkoutTools>();
        services.AddScoped<IProgressTools, ProgressTools>();
        services.AddScoped<IVisionTools, VisionTools>();

        // Register agents
        services.AddScoped<NutritionAgent>();
        services.AddScoped<WorkoutAgent>();
        services.AddScoped<VisionNutritionAgent>();
        services.AddScoped<ProgressCheckInAgent>();
        services.AddScoped<CoachSupervisorAgent>();
        services.AddScoped<ICoachChatService, PythonCoachChatService>();

        // AI Chat Client Factory (scoped — resolves user preference per request)
        services.AddScoped<IAIChatClientFactory, AIChatClientFactory>();

        return services;
    }

    private static void AddChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Register two keyed IChatClient singletons: "AzureOpenAI" and "MiniMax"
        services.AddKeyedSingleton<IChatClient>(AIProviderNames.AzureOpenAI, (sp, key) =>
            BuildChatClient(sp, configuration, "AI", ChatClientTransport.AzureOpenAI));
        services.AddKeyedSingleton<IChatClient>(AIProviderNames.MiniMax, (sp, key) =>
            BuildChatClient(sp, configuration, "MiniMax", ChatClientTransport.OpenAICompatible));
        services.AddKeyedSingleton<IChatClient>(AIProviderNames.Xiaomi, (sp, key) =>
            BuildChatClient(sp, configuration, "Xiaomi", ChatClientTransport.OpenAICompatible));

        // Default (non-keyed) IChatClient — used by SemanticKernel plugins that need
        // IChatClient without user-specific routing. Resolves to AzureOpenAI by default.
        services.AddSingleton<IChatClient>(sp =>
            sp.GetRequiredKeyedService<IChatClient>(AIProviderNames.AzureOpenAI));
    }

    private static IChatClient BuildChatClient(
        IServiceProvider sp,
        IConfiguration configuration,
        string configPrefix,
        ChatClientTransport transport)
    {
        var endpoint = configuration[$"{configPrefix}:Endpoint"]
                       ?? throw new InvalidOperationException($"{configPrefix}:Endpoint configuration is missing");
        var apiKey = configuration[$"{configPrefix}:ApiKey"]!;
        var modelId = configuration[$"{configPrefix}:ModelId"] ?? "gpt-4o";

        IChatClient chatClient;
        if (transport == ChatClientTransport.OpenAICompatible)
        {
            // OpenAI-compatible providers use the generic OpenAI client.
            chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) })
                .GetChatClient(modelId)
                .AsIChatClient();
        }
        else
        {
            chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
                .GetChatClient(modelId)
                .AsIChatClient();
        }

        var builder = new ChatClientBuilder(chatClient);
        if (configuration.GetValue<bool>($"{configPrefix}:EnableCaching", true))
        {
            var cache = sp.GetRequiredService<IMemoryCache>();
            var distributedCache = new MemoryCacheAdapter(cache);
            builder.UseDistributedCache(distributedCache);
        }

        builder.UseFunctionInvocation();
        var clientWithMiddleware = builder.Build();

        // Wrap with outer middleware
        var enableMonitoring = configuration.GetValue<bool>("Performance:EnableMonitoring", true);
        var enableLogging = configuration.GetValue<bool>("Performance:EnableLogging", true);
        var enableFilter = configuration.GetValue<bool>("Performance:EnableSensitiveWordFilter", true);

        IChatClient client = clientWithMiddleware;

        if (enableFilter)
        {
            var logger = sp.GetRequiredService<ILogger<SensitiveWordFilterChatClient>>();
            client = new SensitiveWordFilterChatClient(client, logger);
        }

        if (enableMonitoring)
        {
            var logger = sp.GetRequiredService<ILogger<PerformanceMonitorChatClient>>();
            client = new PerformanceMonitorChatClient(client, logger);
        }

        if (enableLogging)
        {
            var logger = sp.GetRequiredService<ILogger<Middleware.LoggingChatClient>>();
            client = new Middleware.LoggingChatClient(client, logger);
        }

        return client;
    }

    private enum ChatClientTransport
    {
        AzureOpenAI,
        OpenAICompatible
    }

    /// <summary>
    /// 简单的内存缓存适配器（演示用）
    /// </summary>
    private class MemoryCacheAdapter : Microsoft.Extensions.Caching.Distributed.IDistributedCache
    {
        private readonly IMemoryCache _memoryCache;

        public MemoryCacheAdapter(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public byte[]? Get(string key)
        {
            return _memoryCache.Get<byte[]>(key);
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(_memoryCache.Get<byte[]>(key));
        }

        public void Set(string key, byte[] value,
            Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
        {
            var memoryCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = options.SlidingExpiration
            };

            _memoryCache.Set(key, value, memoryCacheOptions);
        }

        public Task SetAsync(string key, byte[] value,
            Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            // Memory cache doesn't need explicit refresh
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
