namespace FitTrack.Copilot.Abstractions;

/// <summary>
/// Describes a callable function exposed by a Plugin.
/// This is framework-agnostic (no direct SK dependency).
/// </summary>
public sealed class FunctionDescriptor
{
    /// <summary>Unique function name (e.g., "vision.nutrition.estimate").</summary>
    public string Name { get; }

    /// <summary>One-line summary for UIs/logging.</summary>
    public string Summary { get; }

    /// <summary>
    /// Input spec: names or MIME-like tokens (e.g., "image/*", "hint?").
    /// Use "param?" suffix for optional inputs.
    /// </summary>
    public IReadOnlyList<string> Inputs { get; }

    /// <summary>
    /// Output spec: a short string such as "NutritionResult(JSON)".
    /// </summary>
    public string Outputs { get; }

    /// <summary>
    /// Optional structured parameter schema (key=name, value=description/type hint).
    /// Useful when Inputs is too terse for dynamic UIs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ParameterSchema { get; }

    /// <summary>Notes for safety, limitations, disclaimers.</summary>
    public string? SafetyNotes { get; }

    /// <summary>Free-form metadata (version, owner, category, etc.).</summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public FunctionDescriptor(
        string Name,
        string Summary,
        IEnumerable<string>? Inputs = null,
        string Outputs = "void",
        IReadOnlyDictionary<string, string>? ParameterSchema = null,
        string? SafetyNotes = null,
        IReadOnlyDictionary<string, string>? Metadata = null)
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Function name is required.", nameof(Name));
        if (string.IsNullOrWhiteSpace(Summary))
            throw new ArgumentException("Function summary is required.", nameof(Summary));

        this.Name = Name;
        this.Summary = Summary;
        this.Inputs = (Inputs ?? Array.Empty<string>()).ToArray();
        this.Outputs = Outputs;
        this.ParameterSchema = ParameterSchema;
        this.SafetyNotes = SafetyNotes;
        this.Metadata = Metadata;
    }

    /// <summary>
    /// Returns true if the descriptor marks an input as optional (name ends with '?').
    /// </summary>
    public static bool IsOptional(string inputName) =>
        !string.IsNullOrWhiteSpace(inputName) && inputName.EndsWith("?", StringComparison.Ordinal);

    public override string ToString() => $"{Name} -> {Outputs}";
}