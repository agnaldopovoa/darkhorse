namespace Darkhorse.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string activationUrl, CancellationToken ct = default);
}
