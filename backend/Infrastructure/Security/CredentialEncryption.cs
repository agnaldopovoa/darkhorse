using Darkhorse.Domain.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace Darkhorse.Infrastructure.Security;

public class CredentialEncryption : ICredentialEncryption
{
    private readonly byte[] _masterKey;

    public CredentialEncryption(string masterKeyHex)
    {
        if (string.IsNullOrWhiteSpace(masterKeyHex) || masterKeyHex.Length != 64)
            throw new InvalidOperationException("MASTER_ENCRYPTION_KEY must be exactly 64 hex characters (32 bytes).");
        _masterKey = Convert.FromHexString(masterKeyHex);
    }

    public (byte[] nonce, byte[] ciphertext, byte[] tag) Encrypt(string plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit nonce per AES-GCM spec
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16]; // 128-bit auth tag

        using var aes = new AesGcm(_masterKey, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return (nonce, cipher, tag);
    }

    public string Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag)
    {
        var plain = new byte[ciphertext.Length];
        using var aes = new AesGcm(_masterKey, tagSizeInBytes: 16);
        aes.Decrypt(nonce, ciphertext, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
