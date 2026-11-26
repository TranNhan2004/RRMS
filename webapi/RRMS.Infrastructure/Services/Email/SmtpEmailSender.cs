using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using RRMS.Application.Interfaces.Services.Email;
using RRMS.Application.Models.Email;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Email;

public sealed class SmtpEmailSender : IEmailSenderService
{
    private readonly IOptions<EmailOptions> _options;
    private readonly IHostEnvironment _environment;

    public SmtpEmailSender(IOptions<EmailOptions> options, IHostEnvironment environment)
    {
        _options = options;
        _environment = environment;
    }

    public async Task SendAsync(EmailMessageModel email, CancellationToken cancellationToken)
    {
        var message = CreateMessage(email);
        using var client = new SmtpClient
        {
            Timeout = 10000
        };

        await client.ConnectAsync(
            _options.Value.Host,
            _options.Value.Port,
            _options.Value.UseSsl ? SecureSocketOptions.SslOnConnect : (_environment.IsDevelopment()
                ? SecureSocketOptions.None : SecureSocketOptions.StartTls), cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Value.Username))
        {
            await client.AuthenticateAsync(_options.Value.Username, _options.Value.Password!, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    internal MimeMessage CreateMessage(EmailMessageModel email)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _options.Value.DefaultFromName ?? string.Empty,
            MailboxAddress.Parse(_options.Value.DefaultFromEmail).Address));

        message.To.Add(MailboxAddress.Parse(email.Recipient));
        message.Subject = email.Subject;
        message.Body = new TextPart("html")
        {
            Text = email.HtmlBody
        };
        return message;
    }
}
