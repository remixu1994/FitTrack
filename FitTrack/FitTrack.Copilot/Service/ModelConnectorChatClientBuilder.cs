using Anthropic;
using Azure;
using Azure.AI.OpenAI;
using FitTrack.Copilot.Data;
using FitTrack.Copilot.Middleware;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using OpenAI;
using System.ClientModel;

namespace FitTrack.Copilot.Service;

public sealed class ModelConnectorChatClientBuilder : IModelConnectorChatClientBuilder
{
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public ModelConnectorChatClientBuilder(
        IMemoryCache memoryCache,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _memoryCache = memoryCache;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    public IChatClient Build(TenantModelConnector connector, string apiKey)
    {
        IChatClient inner = connector.Protocol switch
        {
            TenantModelProtocol.OpenAICompatible => BuildOpenAiCompatible(connector, apiKey),
            TenantModelProtocol.AzureOpenAI => BuildAzureOpenAi(connector, apiKey),
            TenantModelProtocol.Anthropic => BuildAnthropic(connector, apiKey),
            _ => throw new InvalidOperationException($"Unsupported connector protocol '{connector.Protocol}'.")
        };

        inner = new ModelUsageTrackingChatClient(
            inner,
            connector,
            _serviceProvider.GetRequiredService<IModelUsageService>(),
            _serviceProvider.GetRequiredService<IModelRequestContextAccessor>(),
            _serviceProvider.GetRequiredService<ILogger<ModelUsageTrackingChatClient>>());

        var builder = new ChatClientBuilder(inner);
        if (_configuration.GetValue("AI:EnableCaching", true))
        {
            builder.UseDistributedCache(new MemoryDistributedCacheAdapter(_memoryCache));
        }

        builder.UseFunctionInvocation();
        var client = builder.Build();

        if (_configuration.GetValue("Performance:EnableSensitiveWordFilter", true))
        {
            client = new SensitiveWordFilterChatClient(client, _serviceProvider.GetRequiredService<ILogger<SensitiveWordFilterChatClient>>());
        }

        if (_configuration.GetValue("Performance:EnableMonitoring", true))
        {
            client = new PerformanceMonitorChatClient(client, _serviceProvider.GetRequiredService<ILogger<PerformanceMonitorChatClient>>());
        }

        if (_configuration.GetValue("Performance:EnableLogging", true))
        {
            client = new Middleware.LoggingChatClient(client, _serviceProvider.GetRequiredService<ILogger<Middleware.LoggingChatClient>>());
        }

        return client;
    }

    private static IChatClient BuildOpenAiCompatible(TenantModelConnector connector, string apiKey)
        => new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = new Uri(connector.BaseUrl)
                })
            .GetChatClient(connector.ModelId)
            .AsIChatClient();

    private static IChatClient BuildAzureOpenAi(TenantModelConnector connector, string apiKey)
        => new AzureOpenAIClient(new Uri(connector.BaseUrl), new AzureKeyCredential(apiKey))
            .GetChatClient(connector.ModelId)
            .AsIChatClient();

    private static IChatClient BuildAnthropic(TenantModelConnector connector, string apiKey)
    {
        var client = new AnthropicClient
        {
            ApiKey = apiKey,
            BaseUrl = connector.BaseUrl
        };

        return client.AsIChatClient(connector.ModelId);
    }
}
