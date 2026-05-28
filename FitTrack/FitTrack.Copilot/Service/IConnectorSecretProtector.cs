namespace FitTrack.Copilot.Service;

public interface IConnectorSecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
