using System.Threading.Channels;
using Microsoft.Extensions.Options;
using MimeKit;
using RRMS.Application.Interfaces.Services.Email;
using RRMS.Application.Models.Email;
using RRMS.Infrastructure.Options;

namespace RRMS.Infrastructure.Services.Email;

public sealed class EmailQueueService : IEmailQueueService
{
    private readonly Channel<EmailMessageModel> _queue;

    public EmailQueueService(IOptions<EmailOptions> options)
    {
        _queue = Channel.CreateBounded<EmailMessageModel>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(EmailMessageModel message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(message.Recipient) ||
            message.Recipient.Contains('\r') ||
            message.Recipient.Contains('\n') ||
            !MailboxAddress.TryParse(message.Recipient, out var recipient) ||
            !recipient.Address.Contains('@') ||
            string.IsNullOrWhiteSpace(message.Subject) ||
            message.Subject.Contains('\r') ||
            message.Subject.Contains('\n') ||
            string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new ArgumentException("Invalid email message.", nameof(message));
        }

        return _queue.Writer.TryWrite(message);
    }

    public IAsyncEnumerable<EmailMessageModel> ReadAllAsync(CancellationToken cancellationToken) =>
        _queue.Reader.ReadAllAsync(cancellationToken);
}
