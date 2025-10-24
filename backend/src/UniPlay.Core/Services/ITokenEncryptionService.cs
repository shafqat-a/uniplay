namespace UniPlay.Core.Services;

/// <summary>
/// Service for encrypting and decrypting OAuth tokens
/// </summary>
public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypt a plaintext token
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypt an encrypted token
    /// </summary>
    string Decrypt(string encryptedText);
}
