using System.Net;
using System.Net.Http;
using AwesomeAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using RRMS.Api.Extensions;
using Xunit;

namespace RRMS.Api.Tests.Extensions;

public class GoogleAuthenticationTests
{
    [Fact]
    public async Task AddApplicationAuthentication_ShouldConfigureGoogleWithExternalCookie_WhenCredentialsAreConfigured()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["Auth:Session:Cookie:Name"] = "__Host-custom-session",
            ["Auth:Session:Cookie:Path"] = "/",
            ["Auth:Session:Cookie:SameSite"] = "Strict",
            ["Auth:Session:SlidingExpiration"] = "00:30:00",
            ["Auth:Session:AbsoluteExpiration"] = "1.00:00:00",
            ["Auth:Session:ExternalExpiration"] = "00:05:00",
            ["Auth:Google:ClientId"] = "client",
            ["Auth:Google:ClientSecret"] = "secret"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton(new Mock<IConnectionMultiplexer>().Object);
        services.AddApplicationAuthentication(config);
        using var provider = services.BuildServiceProvider();

        // Act
        var scheme = await provider.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync("Google");
        var options = provider.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get("Google");

        // Assert
        scheme.Should().NotBeNull();
        var session = provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Session");
        session.Cookie.Name.Should().Be("__Host-custom-session");
        session.Cookie.Path.Should().Be("/");
        session.Cookie.SameSite.Should().Be(SameSiteMode.Strict);
        options.SignInScheme.Should().Be("External");
        options.CallbackPath.Value.Should().Be("/signin-google");
        options.UsePkce.Should().BeTrue();
        options.SaveTokens.Should().BeFalse();
        options.UserInformationEndpoint.Should().Be("https://openidconnect.googleapis.com/v1/userinfo");
        options.Scope.Should().Contain("openid");
    }

    [Fact]
    public async Task GoogleHandler_ShouldCreateExternalIdentity_WhenProviderCallbackSucceeds()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Session:SlidingExpiration"] = "00:30:00",
            ["Auth:Session:AbsoluteExpiration"] = "1.00:00:00",
            ["Auth:Session:ExternalExpiration"] = "00:05:00",
            ["Auth:Google:ClientId"] = "client",
            ["Auth:Google:ClientSecret"] = "secret"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(new Mock<IDatabase>().Object);
        services.AddSingleton(redis.Object);
        services.AddApplicationAuthentication(config);
        services.PostConfigure<KeyManagementOptions>(options => options.XmlRepository = new TestXmlRepository());
        services.PostConfigure<GoogleOptions>("Google", options => options.Backchannel = new HttpClient(new GoogleBackchannel()));
        using var provider = services.BuildServiceProvider();
        using var challengeScope = provider.CreateScope();
        var challenge = NewContext(challengeScope.ServiceProvider);

        // Act
        await challenge.ChallengeAsync("Google", new AuthenticationProperties
        {
            RedirectUri = "/complete"
        });
        var location = challenge.Response.Headers.Location.ToString();
        var state = QueryHelpers.ParseQuery(new Uri(location).Query)["state"].ToString();
        var correlation = challenge.Response.Headers.SetCookie.Select(value => value!.Split(';')[0]).ToArray();

        // Assert
        state.Should().NotBeNullOrEmpty();
        correlation.Should().NotBeEmpty();

        // Arrange
        using var callbackScope = provider.CreateScope();
        var callback = NewContext(callbackScope.ServiceProvider);
        callback.Request.Path = "/signin-google";
        callback.Request.QueryString = QueryString.Create(new Dictionary<string, string?>
        {
            ["code"] = "valid-code",
            ["state"] = state
        });
        callback.Request.Headers.Cookie = string.Join("; ", correlation);
        var handler = await callbackScope.ServiceProvider.GetRequiredService<IAuthenticationHandlerProvider>().GetHandlerAsync(callback, "Google");

        // Act
        var callbackHandled = await ((IAuthenticationRequestHandler)handler!).HandleRequestAsync();

        // Assert
        callbackHandled.Should().BeTrue();
        callback.Response.Headers.Location.ToString().Should().Be("/complete");
        var externalCookie = callback.Response.Headers.SetCookie.Select(value => value!.Split(';')[0]).Single(value => value.StartsWith("__Host-external="));

        // Arrange
        using var identityScope = provider.CreateScope();
        var identity = NewContext(identityScope.ServiceProvider);
        identity.Request.Headers.Cookie = externalCookie;

        // Act
        var result = await identity.AuthenticateAsync("External");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal!.FindFirst("sub")!.Value.Should().Be("google-subject");
        bool.Parse(result.Principal.FindFirst("email_verified")!.Value).Should().BeTrue();
    }

    private static DefaultHttpContext NewContext(IServiceProvider provider)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.example.org");
        return context;
    }

    private sealed class TestXmlRepository : IXmlRepository
    {
        private readonly List<XElement> _elements = [];

        public IReadOnlyCollection<XElement> GetAllElements() => _elements.ToArray();

        public void StoreElement(XElement element, string friendlyName)
        {
            _elements.Add(element);
        }
    }

    private sealed class GoogleBackchannel : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.RequestUri!.AbsoluteUri.Contains("token", StringComparison.Ordinal)
                ? "{\"access_token\":\"provider-token\",\"token_type\":\"Bearer\"}"
                : "{\"sub\":\"google-subject\",\"email\":\"user@example.org\",\"email_verified\":true}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public void AddApplicationAuthentication_ShouldRejectIncompleteGoogleConfiguration_WhenSecretIsMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:Session:SlidingExpiration"] = "00:30:00",
            ["Auth:Session:AbsoluteExpiration"] = "1.00:00:00",
            ["Auth:Session:ExternalExpiration"] = "00:05:00",
            ["Auth:Google:ClientId"] = "client"
        }).Build();
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddApplicationAuthentication(config);

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }
}
