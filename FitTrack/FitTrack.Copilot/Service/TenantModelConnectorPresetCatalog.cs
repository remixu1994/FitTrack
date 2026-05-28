using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public static class TenantModelConnectorPresetCatalog
{
    public const string XiaomiMimo = "xiaomi-mimo";
    public const string MiniMax = "minimax";
    public const string OpenAICodex = "openai-codex";
    public const string Qwen = "qwen";
    public const string Glm = "glm";
    public const string OpenAICompatible = "openai-compatible";
    public const string AzureOpenAI = "azure-openai";
    public const string Anthropic = "anthropic";

    public const string XiaomiMimoConnectorId = "connector-default-xiaomi-mimo";
    public const string MiniMaxConnectorId = "connector-default-minimax";
    public const string OpenAICodexConnectorId = "connector-default-openai-codex";
    public const string QwenConnectorId = "connector-default-qwen";
    public const string GlmConnectorId = "connector-default-glm";
    public const string OpenAICompatibleConnectorId = "connector-default-openai-compatible";
    public const string AzureOpenAIConnectorId = "connector-default-azure-openai";
    public const string AnthropicConnectorId = "connector-default-anthropic";

    public static readonly IReadOnlyList<TenantModelConnectorPreset> All =
    [
        new(
            XiaomiMimo,
            "Xiaomi MiMo",
            TenantModelProtocol.OpenAICompatible,
            "https://token-plan-cn.xiaomimimo.com/v1",
            "mimo-v2.5",
            LegacyProvider: "Xiaomi",
            ConfigurationSection: "Xiaomi",
            SeedConnectorId: XiaomiMimoConnectorId),
        new(
            MiniMax,
            "MiniMax",
            TenantModelProtocol.OpenAICompatible,
            "https://api.minimax.chat/v1",
            "MiniMax-Text-01",
            LegacyProvider: "MiniMax",
            ConfigurationSection: "MiniMax",
            SeedConnectorId: MiniMaxConnectorId),
        new(
            OpenAICodex,
            "OpenAI Codex",
            TenantModelProtocol.OpenAICompatible,
            "https://api.openai.com/v1",
            "codex-mini-latest",
            SeedConnectorId: OpenAICodexConnectorId),
        new(
            Qwen,
            "Qwen",
            TenantModelProtocol.OpenAICompatible,
            "https://dashscope.aliyuncs.com/compatible-mode/v1",
            "qwen-plus",
            SeedConnectorId: QwenConnectorId),
        new(
            Glm,
            "GLM",
            TenantModelProtocol.OpenAICompatible,
            "https://open.bigmodel.cn/api/paas/v4",
            "glm-4.5",
            SeedConnectorId: GlmConnectorId),
        new(
            OpenAICompatible,
            "OpenAI Compatible",
            TenantModelProtocol.OpenAICompatible,
            "https://api.openai.com/v1",
            "gpt-4.1-mini",
            SeedConnectorId: OpenAICompatibleConnectorId),
        new(
            AzureOpenAI,
            "Azure OpenAI",
            TenantModelProtocol.AzureOpenAI,
            "https://your-resource.openai.azure.com",
            "gpt-4o",
            LegacyProvider: "AzureOpenAI",
            ConfigurationSection: "AI",
            SeedConnectorId: AzureOpenAIConnectorId),
        new(
            Anthropic,
            "Anthropic",
            TenantModelProtocol.Anthropic,
            "https://api.anthropic.com",
            "claude-sonnet-4-20250514",
            SeedConnectorId: AnthropicConnectorId)
    ];

    public static bool TryGet(string key, out TenantModelConnectorPreset preset)
    {
        preset = All.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            ?? null!;
        return preset is not null;
    }

    public static TenantModelConnectorPreset GetRequired(string key)
        => TryGet(key, out var preset)
            ? preset
            : throw new InvalidOperationException($"Unknown connector preset '{key}'.");

    public static string? MapLegacyProviderToConnectorId(string? legacyProvider)
    {
        if (string.IsNullOrWhiteSpace(legacyProvider))
        {
            return null;
        }

        return All.FirstOrDefault(item =>
                string.Equals(item.LegacyProvider, legacyProvider.Trim(), StringComparison.OrdinalIgnoreCase))
            ?.SeedConnectorId;
    }
}
