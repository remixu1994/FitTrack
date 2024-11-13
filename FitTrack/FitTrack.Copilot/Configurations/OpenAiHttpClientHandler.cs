namespace FitTrack.Copilot.Configurations;

public class OpenAiHttpClientHandler(ILogger<OpenAiHttpClientHandler> logger): HttpClientHandler
{  
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {

        var content = await request.Content.ReadAsStringAsync(cancellationToken);
        
        logger.LogInformation("Sending '{Request.Method}' to '{Request.Host}{Request.Path}' with content {Request.Content}",
            request.Method,
            request.RequestUri?.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped),
            request.RequestUri!.PathAndQuery,
            content);
        
        var response = await base.SendAsync(request, cancellationToken);


        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        
        logger.LogInformation(
            "Received '{Response.StatusCodeInt} {Response.StatusCodeString}' with content {Response.Content}",
            (int)response.StatusCode,
            response.StatusCode,
            responseContent
        );
        
        return response;
    }
}