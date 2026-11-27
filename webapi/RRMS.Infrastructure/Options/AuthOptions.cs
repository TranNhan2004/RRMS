namespace RRMS.Infrastructure.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public string FrontendPath { get; set; } = null!;
    public CredentialAuthOptions Credentials { get; set; } = null!;
    public GoogleAuthOptions Google { get; set; } = null!;
    public PendingSessionAuthOptions PendingSession { get; set; } = new();
    public SessionAuthOptions Session { get; set; } = null!;
}

public class CredentialAuthOptions
{
    public string VerificationUrl { get; set; } = null!;
    public TimeSpan RegistrationVerificationTtl { get; set; }
    public TimeSpan RegistrationCompletionTtl { get; set; }
    public TimeSpan RoleSelectionTtl { get; set; }
}

public class SessionAuthOptions
{
    public TimeSpan SlidingExpiration { get; set; }
    public TimeSpan AbsoluteExpiration { get; set; }
    public TimeSpan ExternalExpiration { get; set; }
    public AuthCookieOptions Cookie { get; set; } = null!;
}

public class GoogleAuthOptions
{
    public string ClientId { get; set; } = null!;
    public string ClientSecret { get; set; } = null!;
}

public class PendingSessionAuthOptions
{
    public AuthCookieOptions Cookie { get; set; } = null!;
}

public class AuthCookieOptions
{
    public string Name { get; set; } = null!;
}
