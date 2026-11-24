using RRMS.Application.Models.Users;

namespace RRMS.Application.Interfaces.Services.Users;

public interface IUserQueryService
{
    Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
