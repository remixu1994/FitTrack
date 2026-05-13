using System.Security.Cryptography;
using System.Text;
using FitTrack.Copilot.Data;

namespace FitTrack.Copilot.Service;

public sealed class ModelRequestContext
{
    public string? UserId { get; set; }
    public string? ThreadId { get; set; }
    public string? ConversationMessageId { get; set; }
    public ModelRequestType RequestType { get; set; } = ModelRequestType.Chat;
    public string? UserAgent { get; set; }
    public string? ClientIpHash { get; set; }
    public string? RequestSummary { get; set; }
    public IReadOnlyList<string>? ToolEvents { get; set; }

    public ModelRequestContext Clone()
        => new()
        {
            UserId = UserId,
            ThreadId = ThreadId,
            ConversationMessageId = ConversationMessageId,
            RequestType = RequestType,
            UserAgent = UserAgent,
            ClientIpHash = ClientIpHash,
            RequestSummary = RequestSummary,
            ToolEvents = ToolEvents
        };

    public static string? HashClientIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(ipAddress.Trim()));
        return Convert.ToHexString(hash)[..16];
    }
}

public interface IModelRequestContextAccessor
{
    ModelRequestContext Current { get; }
    IDisposable BeginScope(Action<ModelRequestContext> configure);
}

public sealed class ModelRequestContextAccessor : IModelRequestContextAccessor
{
    private ModelRequestContext _current = new();

    public ModelRequestContext Current => _current;

    public IDisposable BeginScope(Action<ModelRequestContext> configure)
    {
        var previous = _current;
        var next = previous.Clone();
        configure(next);
        _current = next;
        return new Scope(this, previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly ModelRequestContextAccessor _owner;
        private readonly ModelRequestContext _previous;
        private bool _disposed;

        public Scope(ModelRequestContextAccessor owner, ModelRequestContext previous)
        {
            _owner = owner;
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _owner._current = _previous;
            _disposed = true;
        }
    }
}
