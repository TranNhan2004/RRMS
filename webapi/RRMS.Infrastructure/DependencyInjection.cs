using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using RRMS.Application.Interfaces.Contexts;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Infrastructure.Services.Email;
using RRMS.Application.Interfaces.Services.Email;
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
            .AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Email:Host is required.")
            .Validate(options => options.Port is >= 1 and <= 65535, "Email:Port must be between 1 and 65535.")
            .Validate(options => OptionsValidation.ValidEmailAddress(options.DefaultFromEmail), "Email:DefaultFromEmail must be a valid email address.")
            .Validate(options => OptionsValidation.MatchingCredentials(options.Username, options.Password), "Email:Username and Email:Password must both be provided or both omitted.")
            .Validate(options => options.QueueCapacity > 0, "Email:QueueCapacity must be greater than 0.")
            .ValidateOnStart();

        services
            .AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .Validate(options => OptionsValidation.FrontendUrl(options.FrontendPath), "Auth:FrontendPath must be an HTTPS URL, or an HTTP URL on localhost.")
            .Validate<IHostEnvironment>((options, environment) => options.Credentials != null && OptionsValidation.VerificationUrl(options.Credentials.VerificationUrl, environment.IsDevelopment()), "Auth:Credentials:VerificationUrl must be HTTPS, except in the Development environment.")
            .Validate(options => options.Credentials != null && options.Credentials.RegistrationVerificationTtl > TimeSpan.Zero, "Auth:Credentials:RegistrationVerificationTtl must be positive.")
            .Validate(options => options.Credentials != null && options.Credentials.RegistrationCompletionTtl > TimeSpan.Zero, "Auth:Credentials:RegistrationCompletionTtl must be positive.")
            .Validate(options => options.Credentials != null && options.Credentials.RoleSelectionTtl > TimeSpan.Zero, "Auth:Credentials:RoleSelectionTtl must be positive.")
            .Validate(options => options.Session != null && options.Session.SlidingExpiration > TimeSpan.Zero, "Auth:Session:SlidingExpiration must be positive.")
            .Validate(options => options.Session != null && options.Session.AbsoluteExpiration > TimeSpan.Zero, "Auth:Session:AbsoluteExpiration must be positive.")
            .Validate(options => options.Session != null && options.Session.ExternalExpiration > TimeSpan.Zero, "Auth:Session:ExternalExpiration must be positive.")
            .Validate(options => options.Session != null && options.Session.SlidingExpiration <= options.Session.AbsoluteExpiration, "Auth:Session:SlidingExpiration must not exceed AbsoluteExpiration.")
            .Validate(options => options.Session?.Cookie != null && options.Session.Cookie.Name?.StartsWith("__Host-", StringComparison.Ordinal) == true && options.Session.Cookie.Path == "/" && options.Session.Cookie.SecurePolicy == Microsoft.AspNetCore.Http.CookieSecurePolicy.Always, "Auth:Session:Cookie must be a secure __Host- cookie with path /.")
            .Validate(options => options.PendingSession?.Cookie != null && options.PendingSession.Cookie.Name?.StartsWith("__Host-", StringComparison.Ordinal) == true && options.PendingSession.Cookie.Path == "/", "Auth:PendingSession:Cookie must be a __Host- cookie with path /.")
            .Validate(options => options.Google != null && !string.IsNullOrWhiteSpace(options.Google.ClientId), "Auth:Google:ClientId is required.")
            .Validate(options => options.Google != null && !string.IsNullOrWhiteSpace(options.Google.ClientSecret), "Auth:Google:ClientSecret is required.")
            .ValidateOnStart();

        services
            .AddOptions<CsrfOptions>()
            .BindConfiguration(CsrfOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.HeaderName), "Csrf:HeaderName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.CookieTokenName) && options.CookieTokenName.StartsWith("__Host-", StringComparison.Ordinal), "Csrf:CookieTokenName must be a __Host- cookie name.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RequestTokenCookieName), "Csrf:RequestTokenCookieName is required.")
            .Validate(options => options.Secure, "Csrf:Secure must be enabled for the __Host- antiforgery cookie.")
            .Validate(options => Enum.IsDefined(options.SameSite), "Csrf:SameSite must be valid.")
            .ValidateOnStart();

        services
            .AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .Validate(options => options.AllowedOrigins != null && options.AllowedOrigins.All(OptionsValidation.CorsOrigin), "Cors:AllowedOrigins must contain explicit HTTP or HTTPS origins.")
            .Validate(options => OptionsValidation.NonEmptyValues(options.AllowedMethods), "Cors:AllowedMethods must not contain empty values.")
            .Validate(options => OptionsValidation.NonEmptyValues(options.AllowedHeaders), "Cors:AllowedHeaders must not contain empty values.")
            .Validate(options => options.AllowedOrigins == null || options.AllowedOrigins.Length == 0 || (options.AllowedMethods?.Length > 0 && options.AllowedHeaders?.Length > 0), "CORS methods and headers are required when origins are configured.")
            .ValidateOnStart();

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
        var dbConnectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Connection string of 'MySql' not found.");


        services.AddSingleton<AuditSaveChangesInterceptor>();

        var serverVersion = ServerVersion.AutoDetect(dbConnectionString);

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

            options
                .UseMySql(dbConnectionString, new MySqlServerVersion(serverVersion))
                .AddInterceptors(interceptor);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Export
        services.AddSingleton<IExportQueueService, ExportQueueService>();
        services.AddHostedService<ExportBackgroundService>();

        // Audit Log
        services.AddSingleton<IAuditLogBackgroundQueue, AuditLogBackgroundQueue>();
        services.AddHostedService<AuditLogBackgroundService>();

        var cacheConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string of 'Redis' not found.");

        // Cache service
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(cacheConnectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICacheService, RedisService>();

        // Email
        services.AddSingleton<IEmailQueueService, EmailQueueService>();
        services.AddSingleton<IEmailSenderService, SmtpEmailSender>();
        services.AddHostedService<SmtpEmailBackgroundService>();

        // Authentication
        services.AddSingleton<IPasswordService, Argon2PasswordService>();
        services.AddSingleton<IAuthenticationEmailService, AuthenticationEmailService>();
        services.AddScoped<IAuthenticationDataService, AuthenticationDataService>();
        services.AddScoped<IGoogleIdentityCommandService, GoogleIdentityCommandService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<ISessionManagementService, SessionManagementService>();
        services.AddScoped<AuthenticationSessionService>();
        services.AddScoped<AuthenticationEventLogger>();
        services.AddScoped<GoogleAuthenticationService>();
        services.AddSingleton<IAuthenticationChallengeStore, AuthenticationChallengeStore>();

        // Context
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<ICurrentRequestContext, CurrentRequestContext>();

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
        services.AddScoped<IUserCredentialRepository, UserCredentialRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IFileStorageRepository, FileStorageRepository>();

        return services;
    }
}
