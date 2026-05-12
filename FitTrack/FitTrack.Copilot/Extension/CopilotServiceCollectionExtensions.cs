using FitTrack.Copilot.Agents;
using FitTrack.Copilot.AI.Orchestrator;
using FitTrack.Copilot.AI.Plugins;
using FitTrack.Copilot.AI.RAG;
using FitTrack.Copilot.AI.Tooling;
using FitTrack.Copilot.Configuration;
using FitTrack.Copilot.Service;

namespace FitTrack.Copilot.Extension;

public static class CopilotServiceCollectionExtensions
{
    public static IServiceCollection AddCopilotServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PromptOptions>(configuration.GetSection("Prompts"));
        services.AddSingleton<PromptLoader>();
        services.AddSingleton<IConnectorSecretProtector, ConnectorSecretProtector>();

        services.AddScoped<ITenantBootstrapService, TenantBootstrapService>();
        services.AddScoped<ITenantModelConnectorService, TenantModelConnectorService>();
        services.AddScoped<IModelConnectorChatClientBuilder, ModelConnectorChatClientBuilder>();
        services.AddScoped<IAIChatClientFactory, AIChatClientFactory>();

        services.AddTransient<VisionFoodRecognitionPlugin>();
        services.AddTransient<TextFoodRecognitionPlugin>();
        services.AddTransient<NutritionPlugin>();
        services.AddTransient<WorkoutPlugin>();
        services.AddTransient<HealthDataPlugin>();

        services.AddTransient<FitnessRAGService>();
        services.AddTransient<FoodNutritionOrchestrator>();

        services.AddScoped<IFoodAiService, FoodAiService>();
        services.AddScoped<IFoodRecordService, FoodRecordService>();
        services.AddScoped<IFitnessService, FitnessService>();
        services.AddScoped<IWorkoutSessionService, WorkoutSessionService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IConversationMemory, ConversationMemory>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        services.AddScoped<INutritionTools, NutritionTools>();
        services.AddScoped<IWorkoutTools, WorkoutTools>();
        services.AddScoped<IProgressTools, ProgressTools>();
        services.AddScoped<IVisionTools, VisionTools>();

        services.AddScoped<NutritionAgent>();
        services.AddScoped<WorkoutAgent>();
        services.AddScoped<VisionNutritionAgent>();
        services.AddScoped<ProgressCheckInAgent>();
        services.AddScoped<CoachSupervisorAgent>();
        services.AddScoped<ICoachChatService, CoachSupervisorAgent>();

        return services;
    }
}
