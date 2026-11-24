using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Authentication;

public class AuthenticationEmailService : IAuthenticationEmailService
{
    private readonly SmtpOptions _options;
    private readonly IHostEnvironment _environment;

    public AuthenticationEmailService(IOptions<AuthOptions> options, IHostEnvironment environment)
    {
        _options = options.Value.Credentials.Smtp;
        _environment = environment;
    }

    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || _options.Port is < 1 or > 65535 ||
            !MailboxAddress.TryParse(_options.From, out _) ||
            (_options.UseAuthentication && (string.IsNullOrWhiteSpace(_options.UserName) || string.IsNullOrEmpty(_options.Password))) ||
            (!_options.RequireStartTls && !_environment.IsEnvironment("Local")))
            throw new InvalidOperationException("Invalid authentication email configuration.");
    }

    public async Task SendVerificationAsync(string recipient, string verificationLink, CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_options.From));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = "Verify your email";
        message.Body = new TextPart("plain") { Text = "Open this link to verify your email: " + verificationLink };
        using var client = new SmtpClient { Timeout = 10000 };
        await client.ConnectAsync(_options.Host, _options.Port,
            _options.RequireStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
        if (_options.UseAuthentication) await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
