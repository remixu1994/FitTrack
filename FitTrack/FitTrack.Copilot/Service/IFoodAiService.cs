using System.Text.RegularExpressions;
using FitTrack.Copilot.Abstractions.Models;
using Microsoft.AspNetCore.Components;

namespace FitTrack.Copilot.Service;

// IFoodAiService.cs
public interface IFoodAiService
{
    Task<NutritionResult> AnalyzeAsync(FoodRequest req, CancellationToken ct = default);
}

public class FoodRequest
{
    public string? Text { get; set; }              // 作为 hint 传给后端
    public string? ImageDataUrl { get; set; }      // data:<mime>;base64,....
    public string? ServiceId { get; set; }         // 可选
    public string? ModelId { get; set; }           // 可选
}

public class FoodResponse
{
    public string Brief { get; set; } = "Here is the estimate:";
    public NutritionResult Nutrition { get; set; } = new();
}

// FoodAiServiceMock.cs (demo 用，后续替换为你的后端调用)
public sealed class FoodAiServiceHttp : IFoodAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly NavigationManager _nav;

    public FoodAiServiceHttp(IHttpClientFactory httpClientFactory, NavigationManager nav)
    {
        _httpClientFactory = httpClientFactory;
        _nav = nav;
    }

    public async Task<NutritionResult> AnalyzeAsync(FoodRequest req, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("FoodAI");
        // 让相对路径可用（Blazor Server/WASM 皆可）
        client.BaseAddress ??= new Uri(_nav.BaseUri);

        using var form = new MultipartFormDataContent();

        // 1) 处理图片：data URL -> bytes + contentType
        if (!string.IsNullOrWhiteSpace(req.ImageDataUrl))
        {
            var (bytes, contentType) = FromDataUrl(req.ImageDataUrl!);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "image", $"upload.{MimeToExt(contentType)}");
        }

        // 2) 文字提示（hint）
        if (!string.IsNullOrWhiteSpace(req.Text))
            form.Add(new StringContent(req.Text!), "hint");

        // 3) 其它可选参数
        if (!string.IsNullOrWhiteSpace(req.ServiceId))
            form.Add(new StringContent(req.ServiceId!), "serviceId");

        if (!string.IsNullOrWhiteSpace(req.ModelId))
            form.Add(new StringContent(req.ModelId!), "modelId");

        // 4) 发起请求
        using var res = await client.PostAsync("/copilot/vision/estimate", form, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Analyze failed: {(int)res.StatusCode} {res.StatusCode} - {msg}");
        }

        var result = await res.Content.ReadFromJsonAsync<NutritionResult>(cancellationToken: ct)
                     ?? new NutritionResult();

        return result;
    }

    // ---------------- helpers ----------------

    private static (byte[] bytes, string contentType) FromDataUrl(string dataUrl)
    {
        // data:image/png;base64,xxxx
        var m = Regex.Match(dataUrl, @"^data:(?<ct>[^;]+);base64,(?<b64>.+)$",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        if (!m.Success)
            throw new ArgumentException("Invalid data URL", nameof(dataUrl));

        var contentType = m.Groups["ct"].Value;
        var bytes = Convert.FromBase64String(m.Groups["b64"].Value);
        return (bytes, contentType);
    }

    private static string MimeToExt(string mime)
    {
        return mime.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => "jpg",
            "image/png"                  => "png",
            "image/webp"                 => "webp",
            "image/gif"                  => "gif",
            "image/bmp"                  => "bmp",
            _                            => "bin"
        };
    }
}