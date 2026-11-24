namespace RRMS.Infrastructure.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public CredentialAuthOptions Credentials { get; set; } = new();
    public GoogleAuthOptions Google { get; set; } = new();
}

public class CredentialAuthOptions
{
    public string VerificationUrl { get; set; } = string.Empty;
    public SmtpOptions Smtp { get; set; } = new();
    public TimeSpan RegistrationVerificationTtl { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan RegistrationCompletionTtl { get; set; } = TimeSpan.FromMinutes(10);
    public TimeSpan RoleSelectionTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TokenOptions AccessToken { get; set; } = new() { Audience = "rrms-api", Expiration = TimeSpan.FromMinutes(10) };
    public TokenOptions RefreshToken { get; set; } = new() { Audience = "rrms-auth", Expiration = TimeSpan.FromDays(7) };
    public bool SecureCookies { get; set; } = true;
    public string SameSite { get; set; } = "Lax";
    public string RefreshCookiePath { get; set; } = "/api/v1/auth";
}

public class TokenOptions
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "rrms";
    public string Audience { get; set; } = string.Empty;
    public TimeSpan Expiration { get; set; }
}

public class GoogleAuthOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string RoleSelectionUrl { get; set; } = string.Empty;
    public string[] AllowedOrigins { get; set; } = [];
    public TimeSpan RequestTtl { get; set; } = TimeSpan.FromMinutes(5);
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool UseAuthentication { get; set; } = true;
    public bool RequireStartTls { get; set; } = true;
}
