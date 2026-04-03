using System.Net.Http.Json;

namespace FitTrack.Copilot.Service;

public sealed class PythonCoachClient : IPythonCoachClient
{
    private readonly HttpClient _httpClient;

    public PythonCoachClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PythonChatResponse> SendAsync(PythonChatRequest request, CancellationToken ct = default)
    {
        using var response = await _httpClient.PostAsJsonAsync("/internal/agent/chat", request, cancellationToken: ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PythonChatResponse>(cancellationToken: ct);
        if (payload is null)
        {
            throw new InvalidOperationException("Python agent returned an empty response.");
        }

        return payload;
    }
}
