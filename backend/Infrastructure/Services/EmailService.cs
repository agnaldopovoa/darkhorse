using Darkhorse.Domain.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Darkhorse.Infrastructure.Services;

public class EmailService(IConfiguration config, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendVerificationEmailAsync(string toEmail, string activationUrl, CancellationToken ct = default)
    {
        var smtpHost = config["SMTP_HOST"] ?? "localhost";
        var smtpPort = int.TryParse(config["SMTP_PORT"], out var port) ? port : 1025;
        var smtpUser = config["SMTP_USER"];
        var smtpPass = config["SMTP_PASS"];
        var fromAddress = config["SMTP_FROM"] ?? "noreply@darkhorse.local";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Activate your Darkhorse account";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildVerificationEmailHtml(activationUrl),
            TextBody = $"Activate your Darkhorse account by visiting: {activationUrl}\n\nThis link expires in 24 hours."
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            // For Mailpit (dev) or any unauthenticated SMTP server, use None/Auto
            var sslOption = (!string.IsNullOrEmpty(smtpUser)) ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None;
            await client.ConnectAsync(smtpHost, smtpPort, sslOption, ct);

            if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
                await client.AuthenticateAsync(smtpUser, smtpPass, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Verification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to {Email}", toEmail);
            throw;
        }
    }

    private static string BuildVerificationEmailHtml(string activationUrl) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Activate your Darkhorse account</title>
        </head>
        <body style="margin:0;padding:0;background:#0a0a0b;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#0a0a0b;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="520" cellpadding="0" cellspacing="0" style="background:#111113;border:1px solid #1e1e22;border-radius:12px;overflow:hidden;">
                  <!-- Header -->
                  <tr>
                    <td style="padding:32px 40px 24px;border-bottom:1px solid #1e1e22;">
                      <h1 style="margin:0;font-size:22px;font-weight:700;letter-spacing:-0.5px;color:#ffffff;">
                        DARKHORSE
                      </h1>
                    </td>
                  </tr>
                  <!-- Body -->
                  <tr>
                    <td style="padding:36px 40px 28px;">
                      <h2 style="margin:0 0 12px;font-size:20px;font-weight:600;color:#ffffff;">
                        Activate your account
                      </h2>
                      <p style="margin:0 0 24px;font-size:14px;color:#8b8b99;line-height:1.6;">
                        You're one step away from accessing your Darkhorse trading dashboard.
                        Click the button below to verify your email address.
                        This link is valid for <strong style="color:#c8c8d4;">24 hours</strong>.
                      </p>
                      <a href="{activationUrl}"
                         style="display:inline-block;padding:13px 28px;background:#6366f1;color:#ffffff;text-decoration:none;
                                border-radius:8px;font-size:14px;font-weight:600;letter-spacing:0.2px;">
                        Verify Email Address
                      </a>
                      <p style="margin:24px 0 0;font-size:12px;color:#5a5a6a;line-height:1.5;">
                        If you didn't create a Darkhorse account, you can safely ignore this email.
                        <br/>
                        Having trouble? Copy and paste this URL into your browser:<br/>
                        <span style="color:#6366f1;word-break:break-all;">{activationUrl}</span>
                      </p>
                    </td>
                  </tr>
                  <!-- Footer -->
                  <tr>
                    <td style="padding:20px 40px;border-top:1px solid #1e1e22;">
                      <p style="margin:0;font-size:11px;color:#3d3d4a;">
                        &copy; {DateTime.UtcNow.Year} Darkhorse Trading Platform. All rights reserved.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
