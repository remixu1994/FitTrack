using FitTrack.Copilot.Abstractions;

namespace FitTrack.Copilot.Api;


public static class HttpMultipartHelper
{
    /// <summary>
    /// Parse multipart/form-data into List&lt;FilePart&gt; (image/audio/etc.) and inputs (string values).
    /// Expected file field name: any (we take all files).
    /// </summary>
    public static async Task<(List<FilePart> files, Dictionary<string, object?> inputs)> ReadAsync(HttpRequest req, CancellationToken ct)
    {
        if (!req.HasFormContentType)
            throw new InvalidOperationException("multipart/form-data required.");

        var form = await req.ReadFormAsync(ct);

        // files
        var files = new List<FilePart>();
        foreach (var f in form.Files)
        {
            using var s = f.OpenReadStream();
            using var ms = new MemoryStream();
            await s.CopyToAsync(ms, ct);
            files.Add(new FilePart(ms.ToArray(), f.ContentType, f.FileName));
        }

        // inputs
        var inputs = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in form)
        {
            // 注意：IFormCollection 同名多个值时，这里取 ToString()（逗号拼接）
            inputs[kv.Key] = kv.Value.ToString();
        }

        return (files, inputs);
    }
}