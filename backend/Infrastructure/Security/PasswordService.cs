using Darkhorse.Domain.Interfaces.Services;
using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;

namespace Darkhorse.Infrastructure.Security;

public class PasswordService : IPasswordService
{
    private const int HashLength = 32;
    private const int SaltLength = 16;
    private const int Parallelism = 4;
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;

    public (string hash, string salt) HashPassword(string plainPassword)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(SaltLength);
        var hashBytes = ComputeHash(plainPassword, saltBytes);
        return (Convert.ToHexString(hashBytes), Convert.ToHexString(saltBytes));
    }

    public bool VerifyPassword(string plainPassword, string hash, string salt)
    {
        var saltBytes = Convert.FromHexString(salt);
        var expectedHash = ComputeHash(plainPassword, saltBytes);
        var actualHash = Convert.FromHexString(hash);
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = Parallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };
        return argon2.GetBytes(HashLength);
    }
}
