using System.Threading.RateLimiting;
using System.Security.Claims;
using RRMS.Domain.Constants.Policies;
using RRMS.Infrastructure.Options;

namespace RRMS.Api.Extensions;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddApplicationRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetRequiredSection(RateLimiterOptions.SectionName)
            .Get<RateLimiterOptions>()!;

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.AddPolicy(RateLimiterPolicies.Standard, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetIdentityPartition(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.Standard.PermitLimit,
                        Window = options.Standard.Window,
                        SegmentsPerWindow = options.Standard.SegmentsPerWindow,
                        QueueLimit = options.Standard.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            rateLimiterOptions.AddPolicy(RateLimiterPolicies.Export, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetIdentityPartition(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.Export.PermitLimit,
                        Window = options.Export.Window,
                        SegmentsPerWindow = options.Export.SegmentsPerWindow,
                        QueueLimit = options.Export.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));

            rateLimiterOptions.AddPolicy(RateLimiterPolicies.Auth, context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    GetClientIpAddress(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.Auth.PermitLimit,
                        Window = options.Auth.Window,
                        SegmentsPerWindow = options.Auth.SegmentsPerWindow,
                        QueueLimit = options.Auth.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    private static string GetIdentityPartition(HttpContext context)
    {
        var subject = context.User.Identities
            .Where(identity => identity.IsAuthenticated)
            .Select(identity => identity.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return subject is null ? $"ip:{GetClientIpAddress(context)}" : $"user:{subject}";
    }

    private static string GetClientIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
