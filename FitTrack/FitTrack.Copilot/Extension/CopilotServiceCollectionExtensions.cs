using FitTrack.Copilot.Abstractions;
using FitTrack.Copilot.Agent;
using FitTrack.Copilot.Configurations;
using FitTrack.Copilot.SemanticKernel.Plugins;
using FitTrack.Copilot.SemanticKernel.Tooling;
using Microsoft.SemanticKernel;

namespace FitTrack.Copilot.Extension;

public static class CopilotServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PromptOptions>(configuration.GetSection("Prompts"));
        services.AddSingleton<PromptLoader>();
        services.AddKernelServices(configuration);
        services.AddTransient<ImageNutritionAgent>();
        services.AddTransient<VisionNutritionPlugin>();
        services.AddScoped<IAgent>(sp => sp.GetRequiredService<ImageNutritionAgent>());
        return services;
    }

    private static void AddKernelServices(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = configuration.GetSection("AI").Get<AiOptions>()!;
        foreach (var aiProvider in aiOptions.Providers)
        {
            if (aiProvider.Code == "azure-openai")
            {
                var apiKey = File.ReadAllText(@"D:\AI\ApiKey.txt");
                aiProvider.ApiKey = apiKey;
            }

            var providerRegister = AiProviderRegisterFactory.Create(aiProvider!.AiType);

            providerRegister.Register(services, aiProvider, aiOptions.DefaultProvider);
        }
    }

    /// <summary>
    /// New version of DI Implement.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    private static void AddKernelServicesV2(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = configuration.GetSection("AI").Get<AiOptions>()!;
        var azureOpenai = aiOptions.Providers.Find(x => x.Code == "azure-openai")!;
        var apiKey = File.ReadAllText(@"D:\AI\ApiKey.txt");
        azureOpenai.ApiKey = apiKey;
        services.AddKernel()
            .AddOpenAIChatCompletion(modelId: azureOpenai.GetChatCompletionApiService().ModelId,
            apiKey: azureOpenai.ApiKey,
            serviceId: "openai");
    }
    
}