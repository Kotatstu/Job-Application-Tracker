using backend.Interfaces;
using Microsoft.AspNetCore.DataProtection;

public class TokenProtector : ITokenProtector
{
    private readonly IDataProtector _protector;

    public TokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("MailboxRefreshToken.v1");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
}