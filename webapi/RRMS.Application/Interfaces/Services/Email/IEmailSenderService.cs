using RRMS.Application.Models.Email;

namespace RRMS.Application.Interfaces.Services.Email;

public interface IEmailSenderService
{
    Task SendAsync(EmailMessageModel message, CancellationToken cancellationToken);
}