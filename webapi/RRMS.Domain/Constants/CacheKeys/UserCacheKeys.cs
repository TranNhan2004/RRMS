namespace RRMS.Domain.Constants.CacheKeys;

public static class UserCacheKeys
{
    private const string GetCurrentUser = "rrms:users:current:v2:{id}";

    public static CacheKeyObject GetCurrentUserKey(string id) => new()
    {
        Key = GetCurrentUser.Replace("{id}", id),
        Expiration = CacheKeyObject.GetJitter(TimeSpan.FromMinutes(30), 300)
    };
}
