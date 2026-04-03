namespace FitTrack.Copilot.Service;

public interface IPythonCoachClient
{
    Task<PythonChatResponse> SendAsync(PythonChatRequest request, CancellationToken ct = default);
}
