using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public class AIChatClientProvider : IAIChatClientProvider
{
    private const string DefaultProvider = AIProviderNames.AzureOpenAI;

    private readonly IProfileService _profileService;
    private readonly IServiceProvider _serviceProvider;

    public AIChatClientProvider(IProfileService profileService, IServiceProvider serviceProvider)
    {
        _profileService = profileService;
        _serviceProvider = serviceProvider;
    }

    public string CurrentProvider
    {
        get
        {
            // This must be called within a scoped context where UserId is available
            // The actual user context comes from the calling code
            return DefaultProvider;
        }
    }

    public IChatClient ChatClient
    {
        get
        {
            var provider = CurrentProvider;
            return _serviceProvider.GetRequiredKeyedService<IChatClient>(provider);
        }
    }
}
