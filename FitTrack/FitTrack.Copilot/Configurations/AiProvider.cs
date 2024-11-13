namespace FitTrack.Copilot.Configurations;

public  class AiProvider
{
    /// <summary>
    /// AI 服务提供商名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// AI 服务提供商编码
    /// </summary>
    public string Code { get; set; }

    public string ApiKey { get; set; }

    public string ApiEndpoint { get; set; }
    
    
    public  AiProviderType AiType { get; set; }

    public List<ApiService> ApiServices { get; set; }

    public ApiService? GetEmbeddingApiService() => GetApiService("embeddings");

    public ApiService? GetChatCompletionApiService() => GetApiService("chat-completions");

    private ApiService? GetApiService(string apiServiceName) => ApiServices.FirstOrDefault(x => x.Name == apiServiceName);
}