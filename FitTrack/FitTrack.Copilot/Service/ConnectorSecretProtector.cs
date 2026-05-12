using Microsoft.AspNetCore.DataProtection;

namespace FitTrack.Copilot.Service;

public sealed class ConnectorSecretProtector : IConnectorSecretProtector
{
    private readonly IDataProtector _protector;

    public ConnectorSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("FitTrack.Copilot.TenantModelConnector.ApiKey");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}
