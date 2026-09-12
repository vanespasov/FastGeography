namespace FastGeography.Server.Services;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email (not sent — SMTP not configured). To: {To}, Subject: {Subject}, Body: {Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
