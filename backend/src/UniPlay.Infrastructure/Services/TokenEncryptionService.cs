using Microsoft.AspNetCore.DataProtection;
using UniPlay.Core.Services;

namespace UniPlay.Infrastructure.Services;

/// <summary>
/// Token encryption service using ASP.NET Core Data Protection API
/// </summary>
public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly IDataProtector _protector;

    public TokenEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("UniPlay.TokenEncryption");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        return _protector.Protect(plainText);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        return _protector.Unprotect(encryptedText);
    }
}
