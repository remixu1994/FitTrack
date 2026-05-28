namespace FitTrack.Copilot.Service;

public static class AppLanguageSupport
{
    public const string English = "en";
    public const string SimplifiedChinese = "zh-CN";
    public const string HeaderName = "X-FitTrack-Language";

    public static string Normalize(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? English
            : value.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? SimplifiedChinese
                : English;

    public static bool IsChinese(string? value) => Normalize(value) == SimplifiedChinese;

    public static string Select(string? languageCode, string english, string simplifiedChinese)
        => IsChinese(languageCode) ? simplifiedChinese : english;

    public static string BuildReplyInstruction(string? languageCode)
        => IsChinese(languageCode)
            ? "Reply in Simplified Chinese. Keep structured JSON keys and schema field names in English unless the schema explicitly requires localized values."
            : "Reply in English. Keep structured JSON keys and schema field names in English unless the schema explicitly requires localized values.";
}
