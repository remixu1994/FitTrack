using FitTrack.Copilot.Api.Contracts;

namespace FitTrack.Copilot.Service;

public interface IProgressService
{
    Task<ProgressSummaryDto> GetSummaryAsync(string userId, CancellationToken ct = default);
}
