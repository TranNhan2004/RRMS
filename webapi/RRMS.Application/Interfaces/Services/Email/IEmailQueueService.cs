using RRMS.Application.Models.Email;

namespace RRMS.Application.Interfaces.Services.Email;

public interface IEmailQueueService
{
    bool TryEnqueue(EmailMessageModel message);
    IAsyncEnumerable<EmailMessageModel> ReadAllAsync(CancellationToken cancellationToken);
}
