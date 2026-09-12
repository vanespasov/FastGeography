namespace FastGeography.IntegrationTests.Support;

using FastGeography.Server.Services;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        Sent.Add((to, subject, body));
        return Task.CompletedTask;
    }

    public void Clear() => Sent.Clear();
}
