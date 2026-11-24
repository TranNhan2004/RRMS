using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RRMS.Infrastructure.Options;
using Xunit;

namespace RRMS.Infrastructure.Tests.Options;

public class OptionsRegistrationTests
{
    [Fact]
    public void AddAppOptions_ShouldBindAuthenticationCorsAndCsrf_WhenSectionsAreConfigured()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Auth:Credentials:VerificationUrl"] = "https://example.org/verify",
            ["Auth:Google:ClientId"] = "google-client",
            ["Cors:AllowedOrigins:0"] = "https://example.org",
            ["Csrf:HeaderName"] = "X-Custom-CSRF"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddAppOptions(configuration);
        using var provider = services.BuildServiceProvider();

        var auth = provider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var cors = provider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var csrf = provider.GetRequiredService<IOptions<CsrfOptions>>().Value;

        auth.Credentials.VerificationUrl.Should().Be("https://example.org/verify");
        auth.Google.ClientId.Should().Be("google-client");
        cors.AllowedOrigins.Should().ContainSingle().Which.Should().Be("https://example.org");
        csrf.HeaderName.Should().Be("X-Custom-CSRF");
    }
}
