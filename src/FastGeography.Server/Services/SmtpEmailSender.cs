namespace FastGeography.Server.Services;

using System.Net;
using System.Net.Mail;

using FastGeography.Server.Options;

using Microsoft.Extensions.Options;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                ? null
                : new NetworkCredential(_options.UserName, _options.Password),
        };

        var from = string.IsNullOrWhiteSpace(_options.From) ? _options.UserName : _options.From;
        using var message = new MailMessage(from, to, subject, body) { IsBodyHtml = false };

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Password reset email sent to {To}", to);
    }
}
