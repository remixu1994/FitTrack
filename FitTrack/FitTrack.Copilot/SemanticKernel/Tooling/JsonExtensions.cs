using System.Text.Json;

namespace FitTrack.Copilot.AI.Tooling;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>
    /// Deserialize string content to T (safe null return).
    /// </summary>
    public static T? Deserialize<T>(this string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Shortcut for object content → T.
    /// </summary>
    public static T? Deserialize<T>(this object? content)
    {
        if (content is null) return default;
        return Deserialize<T>(content.ToString());
    }
}
