using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Cache;
using RRMS.Application.Interfaces.Services.Users;
using RRMS.Application.Models.Users;
using RRMS.Domain.Constants.CacheKeys;
using RRMS.Domain.Exceptions;

namespace RRMS.Application.Services.Users;

public class UserCommandService : IUserCommandService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public UserCommandService(ICurrentUserContext currentUserContext, IUserRepository userRepository, IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<SetUserNameResponseModel> SetUserNameAsync(SetUserNameRequestModel request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || request.UserName.Trim().Length > 64)
        {
            throw new UnprocessableEntityException("Username must contain between 1 and 64 characters.");
        }

        var userId = _currentUserContext.GetRequiredUserId();
        var user = await _userRepository.FindFirstByConditionAsync(
            query => query.Where(item => item.Id == userId), true, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.UserName != null)
        {
            throw new UnprocessableEntityException("Username can only be set once.");
        }

        user.UserName = request.UserName.Trim();
        await _unitOfWork.SaveChangesAsync(userId.ToString(), cancellationToken);
        await _cacheService.RemoveAsync(UserCacheKeys.GetCurrentUserKey(userId.ToString()), cancellationToken);

        return new SetUserNameResponseModel
        {
            UserName = user.UserName
        };
    }
}
