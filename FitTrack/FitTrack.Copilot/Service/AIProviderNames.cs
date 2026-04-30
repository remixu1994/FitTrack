namespace FitTrack.Copilot.Service;

public static class AIProviderNames
{
    public const string AzureOpenAI = "AzureOpenAI";
    public const string MiniMax = "MiniMax";
    public const string Xiaomi = "Xiaomi";

    public static readonly string[] UserSelectable =
    [
        AzureOpenAI,
        MiniMax,
        Xiaomi
    ];
}
