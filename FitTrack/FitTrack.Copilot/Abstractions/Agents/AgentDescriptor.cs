namespace FitTrack.Copilot.Abstractions.Agents;

/// <summary>
/// Immutable description of an Agent for discovery/telemetry/guardrails.
/// </summary>
public sealed class AgentDescriptor
{
    public string Name { get; }
    public string Purpose { get; }
    public IReadOnlyList<string> Capabilities { get; } // e.g. "vision.nutrition.estimate"
    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public AgentDescriptor(
        string Name,
        string Purpose,
        IEnumerable<string>? Capabilities = null,
        IReadOnlyDictionary<string, string>? Metadata = null)
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Agent name is required.", nameof(Name));
        if (string.IsNullOrWhiteSpace(Purpose))
            throw new ArgumentException("Agent purpose is required.", nameof(Purpose));

        this.Name = Name;
        this.Purpose = Purpose;
        this.Capabilities = (Capabilities ?? Array.Empty<string>()).ToArray();
        this.Metadata = Metadata;
    }

    /// <summary>
    /// Quick helper to check whether the agent claims a capability.
    /// </summary>
    public bool Supports(string capability) =>
        !string.IsNullOrWhiteSpace(capability) &&
        Capabilities.Any(c => string.Equals(c, capability, StringComparison.OrdinalIgnoreCase));

    public override string ToString() => $"{Name} ({string.Join(", ", Capabilities)})";
}