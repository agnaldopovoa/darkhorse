namespace Darkhorse.Domain.Interfaces.Services;

public interface ICredentialEncryption
{
    (byte[] nonce, byte[] ciphertext, byte[] tag) Encrypt(string plaintext);
    string Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag);
}
