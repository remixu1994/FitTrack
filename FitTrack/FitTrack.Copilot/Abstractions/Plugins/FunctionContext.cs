namespace FitTrack.Copilot.Abstractions;

/// <summary>
/// Execution-time bag passed to a Function. Carries inputs, files, identity, correlation id.
/// Framework-agnostic (no SK dependency).
/// </summary>
public sealed class FunctionContext
{
    /// <summary>User identifier (tenant-scoped if applicable).</summary>
    public string UserId { get; }

    /// <summary>Trace id for correlation across services.</summary>
    public string? CorrelationId { get; }

    /// <summary>Named inputs (primitive/POCO). Immutable snapshot.</summary>
    public IReadOnlyDictionary<string, object?> Inputs { get; }

    /// <summary>Uploaded files (image/audio/etc.). Immutable snapshot.</summary>
    public IReadOnlyList<FilePart> Files { get; }

    private readonly IReadOnlyDictionary<string, object?> _inputs;
    private readonly IReadOnlyList<FilePart> _files;

    public FunctionContext(
        string userId,
        IReadOnlyDictionary<string, object?>? inputs = null,
        IReadOnlyList<FilePart>? files = null,
        string? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));

        _inputs = inputs ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        _files  = files  ?? new List<FilePart>();
        UserId = userId;
        CorrelationId = correlationId;
        Inputs = _inputs;
        Files  = _files;
    }

    /// <summary>Create a context from an AgentRequest (adapter boundary).</summary>
    public static FunctionContext FromAgentRequest(AgentRequest request)
        => new(
            userId: request.UserId,
            inputs: request.Inputs,
            files: request.Files?.ToList() ?? new List<FilePart>(),
            correlationId: request.CorrelationId);

    #region Input helpers

    /// <summary>Try get and cast a value from Inputs.</summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        if (!_inputs.TryGetValue(key, out var raw) || raw is null) return false;

        // direct cast
        if (raw is T ok) { value = ok; return true; }

        // string → T conversion (common in HTTP inputs)
        try
        {
            if (raw is string s)
            {
                object? converted = typeof(T).IsEnum
                    ? Enum.Parse(typeof(T), s, ignoreCase: true)
                    : Convert.ChangeType(s, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
                value = (T?)converted;
                return true;
            }

            // handle numbers boxed as long/double → target numeric
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (IsNumericType(raw.GetType()) && IsNumericType(target))
            {
                value = (T?)Convert.ChangeType(raw, target);
                return true;
            }
        }
        catch { /* swallow, return false */ }

        return false;
    }

    /// <summary>Get value or default.</summary>
    public T? Get<T>(string key, T? defaultValue = default)
        => TryGet<T>(key, out var v) ? v : defaultValue;

    /// <summary>Get value or throw descriptive error.</summary>
    public T Require<T>(string key, string? errorMessage = null)
    {
        if (TryGet<T>(key, out var v) && v is not null) return v;
        throw new ArgumentException(errorMessage ?? $"Missing or invalid required input '{key}'.");
    }

    private static bool IsNumericType(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t == typeof(byte) || t == typeof(sbyte) ||
               t == typeof(short) || t == typeof(ushort) ||
               t == typeof(int) || t == typeof(uint) ||
               t == typeof(long) || t == typeof(ulong) ||
               t == typeof(float) || t == typeof(double) ||
               t == typeof(decimal);
    }

    #endregion

    #region File helpers

    /// <summary>
    /// Returns (bytes, contentType) for the first file. Optionally asserts a MIME prefix (e.g. "image/").
    /// Throws if no files or MIME mismatch.
    /// </summary>
    public (byte[] bytes, string contentType) RequireFirstFile(string? expectedMimePrefix = null)
    {
        if (_files.Count == 0) throw new ArgumentException("No file provided.");
        var f = _files[0];
        if (!string.IsNullOrWhiteSpace(expectedMimePrefix) &&
            (string.IsNullOrWhiteSpace(f.ContentType) || !f.ContentType.StartsWith(expectedMimePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"File contentType '{f.ContentType}' does not match expected prefix '{expectedMimePrefix}'.");
        }
        return (f.Bytes, f.ContentType ?? "application/octet-stream");
    }

    /// <summary>Enumerate files, optionally filter by MIME prefix (e.g. "image/").</summary>
    public IEnumerable<(byte[] bytes, string contentType, string? fileName)> EnumerateFiles(string? mimePrefix = null)
    {
        foreach (var f in _files)
        {
            if (string.IsNullOrWhiteSpace(mimePrefix) || (f.ContentType?.StartsWith(mimePrefix, StringComparison.OrdinalIgnoreCase) ?? false))
                yield return (f.Bytes, f.ContentType ?? "application/octet-stream", f.FileName);
        }
    }

    #endregion

    #region Builders

    /// <summary>Create a shallow copy with extra/overridden inputs.</summary>
    public FunctionContext WithInputs(params (string key, object? value)[] pairs)
    {
        var dict = new Dictionary<string, object?>(_inputs, StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in pairs) dict[k] = v;
        return new FunctionContext(UserId, dict, _files, CorrelationId);
    }

    /// <summary>Create a shallow copy with replaced files.</summary>
    public FunctionContext WithFiles(IEnumerable<FilePart> files)
        => new(UserId, _inputs, files.ToList(), CorrelationId);

    #endregion
}