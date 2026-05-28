using FitTrack.Copilot.Api.Usda.Models;
using Microsoft.Extensions.Options;

namespace FitTrack.Copilot.Api.Usda;

public class UsdaClient : IUsdaClient
{
    private readonly HttpClient _httpClient;
    private readonly UsdaOptions _options;

    public UsdaClient(HttpClient httpClient, IOptions<UsdaOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<FoodItem?> SearchAsync(string query)
    {
        var body = new SearchRequest
        {
            Query = query,
            PageSize = 1
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"foods/search?api_key={_options.ApiKey}")
        {
            Content = JsonContent.Create(body)
        };

        var response = await _httpClient.SendAsync(req);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
        return result?.Foods?.FirstOrDefault();
    }

    public async Task<FoodDetail?> GetFoodAsync(long fdcId)
    {
        var response = await _httpClient.GetAsync($"food/{fdcId}?api_key={_options.ApiKey}");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FoodDetail>();
    }
}
