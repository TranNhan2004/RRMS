using RRMS.Domain.Constants.Policies;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration)
    {
        var cors = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        if (cors.AllowedOrigins.Any(origin => origin.Contains('*')))
        {
            throw new InvalidOperationException("CORS requires explicit origins.");
        }

        services.AddCors(options => options.AddPolicy(CorsPolicies.Default, policy =>
        {
            policy.WithOrigins(cors.AllowedOrigins)
                .WithMethods(cors.AllowedMethods)
                .WithHeaders(cors.AllowedHeaders);
            if (cors.AllowCredentials)
            {
                policy.AllowCredentials();
            }
        }));
        return services;
    }
}
