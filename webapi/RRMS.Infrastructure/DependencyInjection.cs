using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Application.Interfaces.Services.Cache;
using RRMS.Infrastructure.Contexts;
using RRMS.Infrastructure.Options;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Persistence.Interceptors;
using RRMS.Infrastructure.Repositories;
using RRMS.Infrastructure.Services.Cache;
using RRMS.Application.Interfaces.Services.Audit;
using RRMS.Infrastructure.Services.Audit;
using RRMS.Application.Interfaces.Services.ObjectStorages;
using RRMS.Infrastructure.Services.ObjectStorages;
using RRMS.Application.Interfaces.Services.Exports;
using RRMS.Infrastructure.Services.ExportJobs;

namespace RRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetRequiredSection(RedisOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                $"{RedisOptions.SectionName}:ConnectionString is required.")
            .ValidateOnStart();

        services
            .AddOptions<AuditLogOptions>()
            .BindConfiguration(AuditLogOptions.SectionName)
            .Validate(options => options.RetryCount >= 0, "AuditLog:RetryCount must be greater than or equal to 0.")
            .Validate(options => options.RetryDelayInMsSeconds != null && options.RetryDelayInMsSeconds.Count == options.RetryCount, "AuditLog:RetryDelayInMsSeconds must have the same number of elements as RetryCount.")
            .Validate(options => options.JitterFromMsSeconds >= 0, "AuditLog:JitterFromMsSeconds must be greater than or equal to 0.")
            .Validate(options => options.JitterToMsSeconds >= 0, "AuditLog:JitterToMsSeconds must be greater than or equal to 0.")
            .Validate(options => options.JitterFromMsSeconds <= options.JitterToMsSeconds, "AuditLog:JitterFromMsSeconds must be less than or equal to JitterToMsSeconds.")
            .ValidateOnStart();

        services
            .AddOptions<Options.ExportOptions>()
            .BindConfiguration(Options.ExportOptions.SectionName)
            .Validate(options => options.MaxQueueItems > 0, "Export:MaxQueueItems must be greater than 0.")
            .ValidateOnStart();

        services
            .AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName);

        services
            .AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName);

        services
            .AddOptions<CsrfOptions>()
            .BindConfiguration(CsrfOptions.SectionName);

        services
            .AddOptions<ObjectStorageOptions>()
            .BindConfiguration(ObjectStorageOptions.SectionName)
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
                && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps),
                "ObjectStorage:Endpoint must be an absolute HTTP or HTTPS URL.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.BucketName), "ObjectStorage:BucketName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Region), "ObjectStorage:Region is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.AccessKey), "ObjectStorage:AccessKey is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey), "ObjectStorage:SecretKey is required.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddSingleton<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

            options
                .UseMySql(connectionString, new MySqlServerVersion(new Version(configuration["Database:ServerVersion"] ?? "8.0.36")))
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Export
        services.AddSingleton<IExportQueueService, ExportQueueService>();
        services.AddHostedService<ExportBackgroundService>();

        // Audit Log
        services.AddSingleton<IAuditLogBackgroundQueue, AuditLogBackgroundQueue>();
        services.AddHostedService<AuditLogBackgroundService>();

        // Cache service
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var redisOptions = sp
                .GetRequiredService<IOptions<RedisOptions>>()
                .Value;

            var options = ConfigurationOptions.Parse(redisOptions.ConnectionString);

            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisService>();

        services.AddDataProtection().SetApplicationName("RRMS");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IConnectionMultiplexer>((options, redis) =>
                options.XmlRepository = new RedisXmlRepository(() => redis.GetDatabase(), "rrms:auth:data-protection-keys"));

        services.AddSingleton<IPasswordService, Argon2PasswordService>();
        services.AddSingleton<IAuthenticationEmailService, AuthenticationEmailService>();
        services.AddScoped<IAuthenticationDataService, AuthenticationDataService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<IAuthenticationChallengeStore, AuthenticationChallengeStore>();

        // Context
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

        // Object storage
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var storage = sp.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
            var credentials = new BasicAWSCredentials(storage.AccessKey, storage.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = storage.Endpoint,
                AuthenticationRegion = storage.Region,
                ForcePathStyle = storage.ForcePathStyle
            };
            return new AmazonS3Client(credentials, config);
        });
        services.AddSingleton<IObjectStorageService, RustFsObjectStorageService>();

        // Repositories
        services.AddScoped<IProvinceRepository, ProvinceRepository>();
        services.AddScoped<IWardRepository, WardRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IFileStorageRepository, FileStorageRepository>();

        return services;
    }
}
