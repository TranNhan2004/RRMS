using RRMS.Application.Models.Users;

namespace RRMS.Application.Interfaces.Services.Users;

public interface IUserCommandService
{
    Task<SetUserNameResponseModel> SetUserNameAsync(SetUserNameRequestModel request, CancellationToken cancellationToken = default);
}
