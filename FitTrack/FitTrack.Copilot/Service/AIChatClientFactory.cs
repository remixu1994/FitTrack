using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public class AIChatClientFactory : IAIChatClientFactory
{
    private const string DefaultProvider = AIProviderNames.AzureOpenAI;

    private readonly IProfileService _profileService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIChatClientFactory> _logger;

    public AIChatClientFactory(
        IProfileService profileService,
        IServiceProvider serviceProvider,
        ILogger<AIChatClientFactory> logger)
    {
        _profileService = profileService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IChatClient> CreateAsync(string userId, CancellationToken ct = default)
    {
        var profile = await _profileService.GetOrCreateProfileAsync(userId, null, ct);
        var provider = profile.PreferredAIProvider ?? DefaultProvider;
        var chatClient = _serviceProvider.GetKeyedService<IChatClient>(provider);
        if (chatClient is not null)
        {
            return chatClient;
        }

        _logger.LogWarning(
            "Unknown AI provider {Provider} for user {UserId}. Falling back to {FallbackProvider}.",
            provider,
            userId,
            DefaultProvider);

        return _serviceProvider.GetRequiredKeyedService<IChatClient>(DefaultProvider);
    }
}
