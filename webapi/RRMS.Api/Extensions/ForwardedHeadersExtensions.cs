using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using ConfigurationOptions = RRMS.Infrastructure.Options.ForwardedHeadersOptions;

namespace RRMS.Api.Extensions;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ConfigurationOptions.SectionName);
        var config = section.Get<ConfigurationOptions>() ?? new ConfigurationOptions();
        if (config.KnownProxies == null || !config.KnownProxies.All(value => IPAddress.TryParse(value, out _)))
        {
            throw new InvalidOperationException("ForwardedHeaders:KnownProxies must contain valid IP addresses.");
        }
        if (config.KnownNetworks == null || !config.KnownNetworks.All(value => System.Net.IPNetwork.TryParse(value, out _)))
        {
            throw new InvalidOperationException("ForwardedHeaders:KnownNetworks must contain valid CIDR networks.");
        }

        services.AddOptions<ConfigurationOptions>()
            .Bind(section)
            .Validate(options => options.KnownProxies != null && options.KnownProxies.All(value => IPAddress.TryParse(value, out _)),
                "ForwardedHeaders:KnownProxies must contain valid IP addresses.")
            .Validate(options => options.KnownNetworks != null && options.KnownNetworks.All(value => System.Net.IPNetwork.TryParse(value, out _)),
                "ForwardedHeaders:KnownNetworks must contain valid CIDR networks.")
            .ValidateOnStart();

        services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in config.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
            foreach (var network in config.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });
        return services;
    }
}
