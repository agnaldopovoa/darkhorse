namespace Darkhorse.Domain.Interfaces.Services;

public interface IPasswordService
{
    (string hash, string salt) HashPassword(string plainPassword);
    bool VerifyPassword(string plainPassword, string hash, string salt);
}
