using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace RRMS.Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApplicationAuthorization(this IServiceCollection services)
    {
        return services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
    }
}
