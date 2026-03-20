namespace Darkhorse.Domain.Interfaces.Services;

public interface INotificationService
{
    Task SendInAppAsync(Guid userId, string type, string title, string? body = null, string metadata = "{}", CancellationToken ct = default);
    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
