using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RRMS.Api.Extensions;
using RRMS.Domain.Constants.Authentication;
using RRMS.Application.Interfaces.Services.Authentication;
using RRMS.Application.Models.Authentication;
using RRMS.Infrastructure.Services.Authentication;
using StackExchange.Redis;
using Xunit;

namespace RRMS.Api.Tests.Authentication;

public class RedisFactAttribute : FactAttribute
{
    public RedisFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RRMS_TEST_REDIS")))
        {
            Skip = "Set RRMS_TEST_REDIS to run Redis integration tests.";
        }
    }
}

public class RedisSessionTests
{
    [RedisFact]
    public async Task RenewAsync_ShouldNotRestoreSession_WhenSessionWasRevoked()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new RedisTicketStore(redis);
        var ticket = NewTicket(Guid.NewGuid());
        var key = await store.StoreAsync(ticket);
        (await store.RetrieveAsync(key)).Should().NotBeNull();

        // Act
        await store.RemoveAsync(key);
        await store.RenewAsync(key, ticket);

        // Assert
        (await store.RetrieveAsync(key)).Should().BeNull();
    }

    [RedisFact]
    public async Task RotateAsync_ShouldAllowOnlyOneSuccess_WhenRequestsRace()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new RedisTicketStore(redis);
        var user = Guid.NewGuid();
        var ticket = NewTicket(user);
        var key = await store.StoreAsync(ticket);

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.RotateAsync(key, ticket)));

        // Assert
        results.Count(result => result != null).Should().Be(1);
        (await store.RetrieveAsync(key)).Should().BeNull();
        (await store.FindUserSessionsAsync(user)).Should().HaveCount(1);
        await store.RemoveAllAsync(user);
        (await store.FindUserSessionsAsync(user)).Should().BeEmpty();
    }

    [RedisFact]
    public async Task ConsumeAsync_ShouldAllowOnlyOneSuccess_WhenPendingSessionIsConsumedConcurrently()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new AuthenticationChallengeStore(redis);
        var token = await store.IssueAsync("role-selection", "user:password", TimeSpan.FromMinutes(1));

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.ConsumeAsync("role-selection", token)));

        // Assert
        results.Count(value => value != null).Should().Be(1);
    }

    [RedisFact]
    public async Task RemoveAsync_ShouldPreserveOtherDevice_WhenUserHasMultipleSessions()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new RedisTicketStore(redis);
        var user = Guid.NewGuid();
        var first = await store.StoreAsync(NewTicket(user));
        var second = await store.StoreAsync(NewTicket(user));

        // Act
        await store.RemoveAsync(first);

        // Assert
        (await store.RetrieveAsync(first)).Should().BeNull();
        (await store.RetrieveAsync(second)).Should().NotBeNull();
        await store.RemoveAllAsync(user);
        (await store.RetrieveAsync(second)).Should().BeNull();
    }

    [RedisFact]
    public async Task StoreAsync_ShouldCapTtl_WhenAbsoluteExpirationIsSoonerThanSlidingExpiration()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new RedisTicketStore(redis);
        var ticket = NewTicket(Guid.NewGuid());
        ticket.Properties.Items["absoluteExpires"] = DateTimeOffset.UtcNow.AddSeconds(5).ToString("O");

        // Act
        var key = await store.StoreAsync(ticket);
        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);

        // Assert
        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(5));
        await store.RemoveAsync(key);
    }

    [RedisFact]
    public async Task StoreAsync_ShouldRejectTicket_WhenAbsoluteExpiryHasPassed()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var store = new RedisTicketStore(redis);
        var ticket = NewTicket(Guid.NewGuid());
        ticket.Properties.Items["absoluteExpires"] = DateTimeOffset.UtcNow.AddSeconds(-1).ToString("O");

        // Act
        var action = () => store.StoreAsync(ticket);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [RedisFact]
    public async Task RotateAsync_ShouldInvalidateOldCookie_WhenActiveRoleChanges()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var userId = Guid.NewGuid();
        var data = new Mock<IAuthenticationDataService>();
        data.Setup(x => x.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Tenant", "Landlord"]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton(data.Object);
        services.Configure<RRMS.Infrastructure.Options.AuthOptions>(options => options.Session = new RRMS.Infrastructure.Options.SessionAuthOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpiration = TimeSpan.FromHours(24),
            ExternalExpiration = TimeSpan.FromMinutes(5)
        });
        services.AddApplicationAuthentication(SessionConfiguration());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var cookies = scope.ServiceProvider.GetRequiredService<SessionCookieService>();
        var login = NewContext(scope.ServiceProvider);
        await cookies.SignInAsync(login, new AuthenticationResultResponseModel
        {
            UserId = userId,
            UserName = "user",
            Email = "user@example.com",
            ActiveRole = "Tenant",
            AuthenticationMethod = "password"
        });
        var oldCookie = login.Response.Headers.SetCookie.Single()!.Split(';')[0];
        using var switchScope = provider.CreateScope();
        var request = NewContext(switchScope.ServiceProvider, oldCookie);
        var original = await request.AuthenticateAsync(AuthenticationSchemes.Session);
        original.Succeeded.Should().BeTrue();
        original.Principal!.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Tenant");
        request.User = original.Principal;

        // Act
        var rotationSucceeded = await switchScope.ServiceProvider.GetRequiredService<SessionCookieService>().RotateAsync(request, "Landlord");

        // Assert
        rotationSucceeded.Should().BeTrue();
        var newCookie = request.Response.Headers.SetCookie.Single()!.Split(';')[0];
        using var oldScope = provider.CreateScope();
        using var newScope = provider.CreateScope();
        var oldRequest = NewContext(oldScope.ServiceProvider, oldCookie);
        var newRequest = NewContext(newScope.ServiceProvider, newCookie);
        (await oldRequest.AuthenticateAsync(AuthenticationSchemes.Session)).Succeeded.Should().BeFalse();
        var rotated = await newRequest.AuthenticateAsync(AuthenticationSchemes.Session);
        rotated.Succeeded.Should().BeTrue();
        rotated.Principal!.FindFirst(ClaimTypes.Role)!.Value.Should().Be("Landlord");
        await scope.ServiceProvider.GetRequiredService<RedisTicketStore>().RemoveAllAsync(userId);
        using var revokedScope = provider.CreateScope();
        (await NewContext(revokedScope.ServiceProvider, newCookie).AuthenticateAsync(AuthenticationSchemes.Session)).Succeeded.Should().BeFalse();
    }

    [RedisFact]
    public async Task AuthenticateAsync_ShouldRevokeSession_WhenActiveRoleIsRemoved()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var userId = Guid.NewGuid();
        var data = new Mock<IAuthenticationDataService>();
        data.Setup(x => x.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddSingleton(data.Object);
        services.Configure<RRMS.Infrastructure.Options.AuthOptions>(options => options.Session = new RRMS.Infrastructure.Options.SessionAuthOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpiration = TimeSpan.FromHours(24),
            ExternalExpiration = TimeSpan.FromMinutes(5)
        });
        services.AddApplicationAuthentication(SessionConfiguration());
        using var provider = services.BuildServiceProvider();
        using var loginScope = provider.CreateScope();
        var login = NewContext(loginScope.ServiceProvider);
        await loginScope.ServiceProvider.GetRequiredService<SessionCookieService>().SignInAsync(login,
            new AuthenticationResultResponseModel
            {
                UserId = userId,
                ActiveRole = "Tenant",
                AuthenticationMethod = "password"
            });
        var cookie = login.Response.Headers.SetCookie.Single()!.Split(';')[0];
        data.Setup(x => x.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        using var removedScope = provider.CreateScope();

        // Act
        var removedAuthentication = await NewContext(removedScope.ServiceProvider, cookie).AuthenticateAsync(AuthenticationSchemes.Session);

        // Assert
        removedAuthentication.Succeeded.Should().BeFalse();
        data.Setup(x => x.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        using var restoredScope = provider.CreateScope();
        (await NewContext(restoredScope.ServiceProvider, cookie).AuthenticateAsync(AuthenticationSchemes.Session)).Succeeded.Should().BeFalse();
        await loginScope.ServiceProvider.GetRequiredService<RedisTicketStore>().RemoveAllAsync(userId);
    }

    [RedisFact]
    public async Task AuthenticateAsync_ShouldAcceptCookieAcrossInstances_WhenRedisAndKeysAreShared()
    {
        // Arrange
        using var redis = await ConnectAsync();
        var userId = Guid.NewGuid();
        var data = new Mock<IAuthenticationDataService>();
        data.Setup(x => x.FindEligibleRolesAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(["Tenant"]);
        var keyName = "rrms:test:keys:" + Guid.NewGuid().ToString("N");
        ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDataProtection().SetApplicationName("RRMS-test").PersistKeysToStackExchangeRedis(redis, keyName);
            services.AddSingleton<IConnectionMultiplexer>(redis);
            services.AddSingleton(data.Object);
            services.Configure<RRMS.Infrastructure.Options.AuthOptions>(options => options.Session = new RRMS.Infrastructure.Options.SessionAuthOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpiration = TimeSpan.FromHours(24),
                ExternalExpiration = TimeSpan.FromMinutes(5)
            });
            services.AddApplicationAuthentication(SessionConfiguration());
            return services.BuildServiceProvider();
        }
        using var first = CreateProvider();
        using var second = CreateProvider();
        using var firstScope = first.CreateScope();
        var login = NewContext(firstScope.ServiceProvider);
        await firstScope.ServiceProvider.GetRequiredService<SessionCookieService>().SignInAsync(login, new AuthenticationResultResponseModel
        {
            UserId = userId,
            ActiveRole = "Tenant",
            AuthenticationMethod = "password"
        });
        var cookie = login.Response.Headers.SetCookie.Single()!.Split(';')[0];
        using var secondScope = second.CreateScope();

        // Act
        var authenticated = await NewContext(secondScope.ServiceProvider, cookie).AuthenticateAsync(AuthenticationSchemes.Session);

        // Assert
        authenticated.Succeeded.Should().BeTrue();
        await secondScope.ServiceProvider.GetRequiredService<RedisTicketStore>().RemoveAllAsync(userId);
        await redis.GetDatabase().KeyDeleteAsync(keyName);
    }

    private static IConfiguration SessionConfiguration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Auth:Session:SlidingExpiration"] = "00:30:00",
        ["Auth:Session:AbsoluteExpiration"] = "1.00:00:00",
        ["Auth:Session:ExternalExpiration"] = "00:05:00",
        ["Auth:Session:Cookie:Name"] = "__Host-session",
        ["Auth:Session:Cookie:Path"] = "/",
        ["Auth:Session:Cookie:SecurePolicy"] = "Always",
        ["Auth:Session:Cookie:SameSite"] = "Lax",
        ["Auth:Google:ClientId"] = "client",
        ["Auth:Google:ClientSecret"] = "secret"
    }).Build();

    private static DefaultHttpContext NewContext(IServiceProvider provider, string? cookie = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Request.Scheme = "https";
        if (cookie != null)
        {
            context.Request.Headers.Cookie = cookie;
        }

        return context;
    }

    private static Task<ConnectionMultiplexer> ConnectAsync() => ConnectionMultiplexer.ConnectAsync(Environment.GetEnvironmentVariable("RRMS_TEST_REDIS")!);
    private static AuthenticationTicket NewTicket(Guid user)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", user.ToString()), new Claim("sessionRef", Guid.NewGuid().ToString())], "Session"));
        var properties = new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        properties.Items["absoluteExpires"] = DateTimeOffset.UtcNow.AddHours(24).ToString("O");
        return new AuthenticationTicket(principal, properties, "Session");
    }
}
