namespace FitTrack.Copilot.Abstractions;

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

    /// <summary>Create FilePart from a Stream.</summary>
    public static FilePart FromStream(Stream s)
    {
        using var ms = new MemoryStream();
        s.CopyTo(ms);
        return new FilePart(ms.ToArray(), null, null);
    }
}
