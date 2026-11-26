using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Cache;
using RRMS.Application.Interfaces.Services.Users;
using RRMS.Application.Models.Users;
using RRMS.Domain.Constants.CacheKeys;
using RRMS.Domain.Exceptions;

namespace RRMS.Application.Services.Users;

public class UserQueryService : IUserQueryService
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserRepository _userRepository;
    private readonly ICacheService _cacheService;

    public UserQueryService(ICurrentUserContext currentUserContext, IUserRepository userRepository, ICacheService cacheService)
    {
        _currentUserContext = currentUserContext;
        _userRepository = userRepository;
        _cacheService = cacheService;
    }

    public async Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var id = _currentUserContext.GetRequiredUserId();
        var key = UserCacheKeys.GetCurrentUserKey(id.ToString());
        var cached = await _cacheService.GetAsync<GetCurrentUserResponseModel>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var user = await _userRepository.FindCurrentUserAsync(id, cancellationToken)
            ?? throw new NotFoundException("User not found");

        await _cacheService.SetAsync(key, user, cancellationToken);
        return user;
    }
}
