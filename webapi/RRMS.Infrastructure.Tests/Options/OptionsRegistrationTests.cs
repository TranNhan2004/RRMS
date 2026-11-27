using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Moq;
using RRMS.Infrastructure.Options;
using Xunit;

namespace RRMS.Infrastructure.Tests.Options;

public class OptionsRegistrationTests
{
    [Fact]
    public void AddAppOptions_ShouldBindOptions_WhenConfigurationIsValid()
    {
        // Arrange
        using var provider = CreateProvider(ValidConfiguration());

        // Act
        var auth = provider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var cors = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var email = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
        var csrf = provider.GetRequiredService<IOptions<CsrfOptions>>().Value;

        // Assert
        auth.Credentials.VerificationUrl.Should().Be("https://example.org/verify");
        auth.Google.ClientId.Should().Be("google-client");
        auth.Session.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(30));
        auth.Session.Cookie.Name.Should().Be("custom-session");
        auth.PendingSession.Cookie.Name.Should().Be("custom-pending");
        csrf.RequestTokenCookieName.Should().Be("CUSTOM-XSRF");
        csrf.HeaderName.Should().Be("X-XSRF-TOKEN");
        csrf.CookieTokenName.Should().Be("antiforgery");
        email.Host.Should().Be("smtp.example.org");
        email.QueueCapacity.Should().Be(32);
        cors.AllowedOrigins.Should().ContainSingle().Which.Should().Be("https://example.org");
    }

    [Theory]
    [InlineData("Csrf:RequestTokenCookieName", "")]
    [InlineData("Csrf:CookieTokenName", "__Host-bad")]
    public void CsrfOptions_ShouldRejectInvalidConfiguration_WhenCookieSettingsAreInvalid(string key, string value)
    {
        // Arrange
        var values = ValidConfiguration();
        values[key] = value;
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<CsrfOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void CsrfOptions_ShouldRejectMissingCookie_WhenCookieSectionIsAbsent()
    {
        // Arrange
        var values = ValidConfiguration();
        foreach (var key in values.Keys.Where(key => key.StartsWith("Csrf:", StringComparison.Ordinal)).ToArray())
        {
            values.Remove(key);
        }

        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<CsrfOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("Email:Host", "")]
    [InlineData("Email:Port", "0")]
    [InlineData("Email:Port", "65536")]
    [InlineData("Email:DefaultFromEmail", "invalid")]
    [InlineData("Email:QueueCapacity", "0")]
    [InlineData("Email:Username", "user")]
    [InlineData("Email:Password", "password")]
    public void EmailOptions_ShouldRejectInvalidConfiguration_WhenRequiredValuesAreInvalid(string key, string value)
    {
        // Arrange
        var values = ValidConfiguration();
        values[key] = value;
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<EmailOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("Auth:FrontendPath", "javascript://localhost")]
    [InlineData("Auth:Credentials:VerificationUrl", "invalid")]
    [InlineData("Auth:Credentials:VerificationUrl", "http://example.org/verify")]
    [InlineData("Auth:Credentials:RegistrationVerificationTtl", "00:00:00")]
    [InlineData("Auth:Credentials:RegistrationCompletionTtl", "00:00:00")]
    [InlineData("Auth:Credentials:RoleSelectionTtl", "00:00:00")]
    [InlineData("Auth:Session:SlidingExpiration", "00:00:00")]
    [InlineData("Auth:Session:AbsoluteExpiration", "00:00:00")]
    [InlineData("Auth:Session:ExternalExpiration", "00:00:00")]
    [InlineData("Auth:Session:SlidingExpiration", "2.00:00:00")]
    [InlineData("Auth:Session:Cookie:Name", "__Host-session")]
    [InlineData("Auth:PendingSession:Cookie:Name", "__Host-pending")]
    [InlineData("Auth:Google:ClientId", "")]
    [InlineData("Auth:Google:ClientSecret", "")]
    public void AuthOptions_ShouldRejectInvalidConfiguration_WhenRequiredValuesAreInvalid(string key, string value)
    {
        // Arrange
        var values = ValidConfiguration();
        values[key] = value;
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.org/path")]
    [InlineData("https://user@example.org")]
    [InlineData("not-a-url")]
    public void CorsOptions_ShouldRejectInvalidOrigin_WhenOriginIsNotExplicit(string origin)
    {
        // Arrange
        var values = ValidConfiguration();
        values["Cors:AllowedOrigins:0"] = origin;
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void CorsOptions_ShouldAllowEmptyOrigins_WhenOnlySameOriginIsUsed()
    {
        // Arrange
        var values = ValidConfiguration();
        values.Remove("Cors:AllowedOrigins:0");
        values.Remove("Cors:AllowedMethods:0");
        values.Remove("Cors:AllowedHeaders:0");
        using var provider = CreateProvider(values);

        // Act
        var cors = provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        cors.AllowedOrigins.Should().BeEmpty();
        cors.AllowedMethods.Should().BeEmpty();
        cors.AllowedHeaders.Should().BeEmpty();
    }

    [Fact]
    public void CorsOptions_ShouldRejectMissingMethods_WhenOriginsAreConfigured()
    {
        // Arrange
        var values = ValidConfiguration();
        values.Remove("Cors:AllowedMethods:0");
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void AuthOptions_ShouldAllowHttpVerificationUrl_WhenEnvironmentIsDevelopment()
    {
        // Arrange
        var values = ValidConfiguration();
        values["Auth:Credentials:VerificationUrl"] = "http://localhost:4200/verify";
        using var provider = CreateProvider(values, Environments.Development);

        // Act
        var auth = provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        // Assert
        auth.Credentials.VerificationUrl.Should().Be("http://localhost:4200/verify");
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Local")]
    public void AuthOptions_ShouldRejectHttpVerificationUrl_WhenEnvironmentIsNotDevelopment(string environmentName)
    {
        // Arrange
        var values = ValidConfiguration();
        values["Auth:Credentials:VerificationUrl"] = "http://localhost:4200/verify";
        using var provider = CreateProvider(values, environmentName);

        // Act
        var action = () => provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("Auth:Credentials:")]
    [InlineData("Auth:Session:")]
    [InlineData("Auth:Google:")]
    public void AuthOptions_ShouldRejectMissingSection_WhenRequiredSettingsAreAbsent(string prefix)
    {
        // Arrange
        var values = ValidConfiguration();
        foreach (var key in values.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray())
        {
            values.Remove(key);
        }
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IOptions<AuthOptions>>().Value;

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    [Theory]
    [InlineData("Email:Port", "0")]
    [InlineData("Auth:Session:ExternalExpiration", "00:00:00")]
    [InlineData("Cors:AllowedOrigins:0", "*")]
    [InlineData("Csrf:RequestTokenCookieName", "")]
    public void StartupValidation_ShouldRejectInvalidOptions_WhenApplicationStarts(string key, string value)
    {
        // Arrange
        var values = ValidConfiguration();
        values[key] = value;
        using var provider = CreateProvider(values);

        // Act
        var action = () => provider.GetRequiredService<IStartupValidator>().Validate();

        // Assert
        action.Should().Throw<OptionsValidationException>();
    }

    private static ServiceProvider CreateProvider(Dictionary<string, string?> values, string environmentName = "Production")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);
        services.AddSingleton(environment.Object);
        services.AddAppOptions(configuration);
        return services.BuildServiceProvider();
    }

    private static Dictionary<string, string?> ValidConfiguration() => new()
    {
        ["Csrf:RequestTokenCookieName"] = "CUSTOM-XSRF",
        ["Csrf:HeaderName"] = "X-XSRF-TOKEN",
        ["Csrf:CookieTokenName"] = "antiforgery",
        ["ConnectionStrings:Redis"] = "localhost:6379",
        ["Export:MaxQueueItems"] = "10",
        ["ObjectStorage:Endpoint"] = "https://storage.example.org",
        ["ObjectStorage:BucketName"] = "test-bucket",
        ["ObjectStorage:Region"] = "test-region",
        ["ObjectStorage:AccessKey"] = "test-access",
        ["ObjectStorage:SecretKey"] = "test-secret",
        ["Auth:FrontendPath"] = "https://example.org",
        ["Auth:Credentials:VerificationUrl"] = "https://example.org/verify",
        ["Auth:Credentials:RegistrationVerificationTtl"] = "00:15:00",
        ["Auth:Credentials:RegistrationCompletionTtl"] = "00:10:00",
        ["Auth:Credentials:RoleSelectionTtl"] = "00:05:00",
        ["Auth:PendingSession:Cookie:Name"] = "custom-pending",
        ["Auth:Session:Cookie:Name"] = "custom-session",
        ["Auth:Session:SlidingExpiration"] = "00:30:00",
        ["Auth:Session:AbsoluteExpiration"] = "1.00:00:00",
        ["Auth:Session:ExternalExpiration"] = "00:05:00",
        ["Auth:Google:ClientId"] = "google-client",
        ["Auth:Google:ClientSecret"] = "secret",
        ["Email:Host"] = "smtp.example.org",
        ["Email:Port"] = "587",
        ["Email:DefaultFromEmail"] = "sender@example.org",
        ["Email:QueueCapacity"] = "32",
        ["Cors:AllowedOrigins:0"] = "https://example.org",
        ["Cors:AllowedMethods:0"] = "GET",
        ["Cors:AllowedHeaders:0"] = "Content-Type"
    };
}
