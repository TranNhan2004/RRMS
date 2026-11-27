namespace RRMS.Infrastructure.Options;

public class EmailOptions
{
    public const string SectionName = "Email";
    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string DefaultFromEmail { get; set; } = null!;
    public string? DefaultFromName { get; set; }
    public bool UseSsl { get; set; }
    public int QueueCapacity { get; set; }
}
