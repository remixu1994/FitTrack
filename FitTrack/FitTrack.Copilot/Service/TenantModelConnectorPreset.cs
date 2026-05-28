using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public sealed record TenantModelConnectorPreset(
    string Key,
    string DisplayName,
    TenantModelProtocol Protocol,
    string BaseUrl,
    string ModelId,
    string? LegacyProvider = null,
    string? ConfigurationSection = null,
    string? SeedConnectorId = null,
    IReadOnlyList<string>? ConfigurationSectionAliases = null,
    IReadOnlyList<string>? LegacyProviderAliases = null)
{
    public IEnumerable<string> GetConfigurationSections()
    {
        if (!string.IsNullOrWhiteSpace(ConfigurationSection))
        {
            yield return ConfigurationSection;
        }

        foreach (var alias in ConfigurationSectionAliases ?? [])
        {
            if (!string.IsNullOrWhiteSpace(alias))
            {
                yield return alias;
            }
        }
    }

    public bool MatchesLegacyProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        if (string.Equals(LegacyProvider, provider, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LegacyProviderAliases?.Any(alias => string.Equals(alias, provider, StringComparison.OrdinalIgnoreCase)) == true;
    }
}
