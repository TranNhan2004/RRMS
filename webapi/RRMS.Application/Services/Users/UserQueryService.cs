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
    private readonly IUserRepository _users;
    private readonly ICacheService _cache;

    public UserQueryService(ICurrentUserContext currentUserContext, IUserRepository users, ICacheService cache)
    {
        _currentUserContext = currentUserContext;
        _users = users;
        _cache = cache;
    }

    public async Task<GetCurrentUserResponseModel> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var id = _currentUserContext.UserId;
        var key = UserCacheKeys.GetCurrentUserKey(id.ToString());
        var cached = await _cache.GetAsync<GetCurrentUserResponseModel>(key, cancellationToken);
        if (cached != null) return cached;
        var user = await _users.FindCurrentUserAsync(id, cancellationToken) ?? throw new NotFoundException("User not found");
        await _cache.SetAsync(key, user, cancellationToken);
        return user;
    }
}
