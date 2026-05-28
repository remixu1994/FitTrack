using Microsoft.Extensions.AI;

namespace FitTrack.Copilot.Service;

public interface IAIChatClientFactory
{
    Task<IChatClient> CreateAsync(string userId, CancellationToken ct = default);
}
