using RRMS.Application.Interfaces.Services.Email;
using System.Text.Encodings.Web;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Email;
using RRMS.Infrastructure.Services.Email;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Builds and queues the verification email sent during registration.
/// </summary>
public class AuthenticationEmailService : IAuthenticationEmailService
{
    private readonly IEmailQueueService _queue;

    public AuthenticationEmailService(IEmailQueueService queue)
    {
        _queue = queue;
    }

    public Task SendVerificationAsync(string recipient, string verificationLink, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var encodedLink = HtmlEncoder.Default.Encode(verificationLink);
        var email = new EmailMessageModel
        {
            Recipient = recipient,
            Subject = "Verify your email",
            HtmlBody = $"<p>Open this link to verify your email:</p><p><a href=\"{encodedLink}\">Verify your email</a></p>"
        };
        if (!_queue.TryEnqueue(email))
        {
            throw new InvalidOperationException("Email queue is full.");
        }

        return Task.CompletedTask;
    }
}
