using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using StackExchange.Redis;
using RRMS.Domain.Constants.CacheKeys;

namespace RRMS.Api.Extensions;

public static class RedisDataProtectionKeyStorageExtensions
{
    public static IServiceCollection AddRedisDataProtectionKeyStorage(this IServiceCollection services)
    {
        services
            .AddDataProtection()
            .SetApplicationName("RRMS");

        services
            .AddOptions<KeyManagementOptions>()
            .Configure<IConnectionMultiplexer>((options, redis) =>
                options.XmlRepository = new RedisXmlRepository(() => redis.GetDatabase(), DataProtectionKeyCacheKeys.GetDpkRingKey().Key));

        return services;
    }
}
