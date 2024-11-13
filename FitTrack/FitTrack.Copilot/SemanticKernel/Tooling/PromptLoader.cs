using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace FitTrack.Copilot.SemanticKernel.Tooling;

/// <summary>
/// Loads prompt texts from disk or embedded resources with in-memory caching.
/// </summary>
public sealed class PromptLoader
{
    private readonly PromptOptions _options;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Assembly _resourceAssembly;

    public PromptLoader(IOptions<PromptOptions> options)
    {
        _options = options.Value ?? new PromptOptions();
        _resourceAssembly = _options.ResourceAssembly ?? Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// Load a prompt file by name (e.g., "vision_nutrition.system.md").
    /// Tries (1) culture-specific file; (2) base file; (3) embedded resource.
    /// </summary>
    public async Task<string> LoadAsync(string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Prompt name required.", nameof(name));

        // cache hit
        if (_cache.TryGetValue(name, out var cached)) return cached;

        // 1) culture-specific (e.g., *.zh-CN.md)
        var culture = _options.Culture ?? CultureInfo.CurrentUICulture;
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_options.RootDirectory))
        {
            var withCulture = Path.Combine(_options.RootDirectory, AppendCulture(name, culture));
            var baseFile    = Path.Combine(_options.RootDirectory, name);
            candidates.Add(withCulture);
            candidates.Add(baseFile);
        }

        // 2) embedded resource fallback
        // resource name style: "<DefaultNamespace>.Plugins.SystemPrompt.<file>"
        var resourceCandidates = BuildResourceCandidates(name, culture, _options.ResourcePrefix);

        // Try filesystem candidates
        foreach (var file in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (File.Exists(file))
            {
                using var fs = File.OpenRead(file);
                using var sr = new StreamReader(fs);
                var text = await sr.ReadToEndAsync();
                _cache[name] = text;
                return text;
            }
        }

        // Try embedded resources
        foreach (var res in resourceCandidates)
        {
            await using var stream = _resourceAssembly.GetManifestResourceStream(res);
            if (stream is null) continue;

            using var sr = new StreamReader(stream);
            var text = await sr.ReadToEndAsync();
            _cache[name] = text;
            return text;
        }

        throw new FileNotFoundException($"Prompt '{name}' not found in '{_options.RootDirectory}' or embedded resources.");
    }

    /// <summary>
    /// Load and apply inline placeholders, e.g., {{name}}.
    /// </summary>
    public async Task<string> LoadTemplateAsync(string name, IDictionary<string, string> placeholders, CancellationToken ct = default)
    {
        var text = await LoadAsync(name, ct);
        if (placeholders is null || placeholders.Count == 0) return text;

        foreach (var kv in placeholders)
        {
            text = text.Replace("{{" + kv.Key + "}}", kv.Value ?? string.Empty, StringComparison.Ordinal);
        }
        return text;
    }

    private static string AppendCulture(string fileName, CultureInfo culture)
    {
        var ext = Path.GetExtension(fileName);
        var without = Path.GetFileNameWithoutExtension(fileName);
        return $"{without}.{culture.Name}{ext}";
    }

    private IEnumerable<string> BuildResourceCandidates(string name, CultureInfo culture, string? prefix)
    {
        // default prefix: "FitTrack.Copilot.SemanticKernel.Plugins.SystemPrompt"
        var pfx = string.IsNullOrWhiteSpace(prefix)
            ? $"{_resourceAssembly.GetName().Name}.Plugins.SystemPrompt"
            : prefix.TrimEnd('.');

        yield return $"{pfx}.{AppendCulture(ToResourceName(name), culture)}";
        yield return $"{pfx}.{ToResourceName(name)}";
    }

    private static string ToResourceName(string name) => name.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
}

/// <summary>
/// Options for PromptLoader.
/// </summary>
public sealed class PromptOptions
{
    /// <summary>Root directory for prompt files (absolute or relative to app base).</summary>
    public string? RootDirectory { get; set; } = "src/FitTrack.Copilot.SemanticKernel/Plugins/SystemPrompt";

    /// <summary>Where to look for embedded resources (namespace-like prefix).</summary>
    public string? ResourcePrefix { get; set; } = null; // default to {AssemblyName}.Plugins.SystemPrompt

    /// <summary>Optional culture to try first; defaults to CurrentUICulture.</summary>
    public CultureInfo? Culture { get; set; }

    /// <summary>Override assembly that contains embedded prompt resources.</summary>
    public Assembly? ResourceAssembly { get; set; }
}
