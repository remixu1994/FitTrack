using FitTrack.Copilot.Abstractions.Agents;

namespace FitTrack.Copilot.Abstractions;

public interface IAgent
{
    AgentDescriptor Descriptor { get; }
    Task<AgentResult> ExecuteAsync(AgentRequest request, CancellationToken ct = default);
}

// IFunction.cs
public interface IFunction
{
    FunctionDescriptor Describe();
    Task<object?> InvokeAsync(FunctionContext context, CancellationToken ct = default);
}

/// <summary>Binary payload for a file upload.</summary>
public sealed class FilePart
{
    public byte[] Bytes { get; }
    public string? ContentType { get; }
    public string? FileName { get; }

    public FilePart(byte[] bytes, string? contentType = null, string? fileName = null)
    {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        ContentType = contentType;
        FileName = fileName;
    }

    /// <summary>Create FilePart from a Stream in AgentRequest.Files (assumed at current position).</summary>
    public static FilePart FromStream(Stream s)
    {
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        // We don't have MIME/FileName on a bare Stream; callers can override later if needed.
        return new FilePart(ms.ToArray(), null, null);
    }
}


// AgentRequest/Result (minimal)
public sealed record AgentRequest(
    string UserId,
    string Intent,                    // "vision.nutrition.estimate" / "text.nutrition.parse"
    IReadOnlyDictionary<string,object?> Inputs,
    IReadOnlyList<FilePart>? Files = null,
    string? CorrelationId = null);

public sealed record AgentResult(
    bool Success,
    object? Data,
    string? Message = null);