using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RRMS.Infrastructure.Extensions;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var configuredProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
        var trustedProxies = new List<IPAddress>();
        foreach (var value in configuredProxies)
        {
            if (!IPAddress.TryParse(value, out var address))
            {
                throw new InvalidOperationException("ForwardedHeaders:KnownProxies must contain valid IP addresses.");
            }
            trustedProxies.Add(address);
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var address in trustedProxies)
            {
                options.KnownProxies.Add(address);
            }
        });
        return services;
    }
}
