using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public class AIChatClientFactory : IAIChatClientFactory
{
    private readonly ITenantModelConnectorService _tenantModelConnectorService;
    private readonly IModelConnectorChatClientBuilder _chatClientBuilder;
    private readonly IConnectorSecretProtector _secretProtector;
    private readonly ILogger<AIChatClientFactory> _logger;

    public AIChatClientFactory(
        ITenantModelConnectorService tenantModelConnectorService,
        IModelConnectorChatClientBuilder chatClientBuilder,
        IConnectorSecretProtector secretProtector,
        ILogger<AIChatClientFactory> logger)
    {
        _tenantModelConnectorService = tenantModelConnectorService;
        _chatClientBuilder = chatClientBuilder;
        _secretProtector = secretProtector;
        _logger = logger;
    }

    public async Task<IChatClient> CreateAsync(string userId, CancellationToken ct = default)
    {
        var connector = await _tenantModelConnectorService.ResolveConnectorForUserAsync(userId, ct);
        if (connector is null)
        {
            _logger.LogWarning("No enabled tenant model connector is available for user {UserId}.", userId);
            throw new InvalidOperationException("No enabled tenant model connector is configured for this user.");
        }

        if (string.IsNullOrWhiteSpace(connector.EncryptedApiKey))
        {
            _logger.LogWarning("Connector {ConnectorId} for user {UserId} is missing an API key.", connector.Id, userId);
            throw new InvalidOperationException($"Connector '{connector.DisplayName}' is missing an API key.");
        }

        var apiKey = _secretProtector.Unprotect(connector.EncryptedApiKey);
        return _chatClientBuilder.Build(connector, apiKey);
    }
}
