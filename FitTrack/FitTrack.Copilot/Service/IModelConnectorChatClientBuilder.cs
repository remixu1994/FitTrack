using FitTrack.Copilot.Data;
using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public interface IModelConnectorChatClientBuilder
{
    IChatClient Build(TenantModelConnector connector, string apiKey);
}
