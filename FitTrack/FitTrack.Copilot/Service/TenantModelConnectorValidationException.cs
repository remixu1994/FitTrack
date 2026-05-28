using System.Net;

namespace FitTrack.Copilot.Service;

public sealed class TenantModelConnectorValidationException : Exception
{
    public TenantModelConnectorValidationException(string errorCode, string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }

    public string ErrorCode { get; }

    public HttpStatusCode StatusCode { get; }
}
