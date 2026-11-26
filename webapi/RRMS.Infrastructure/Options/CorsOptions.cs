namespace RRMS.Infrastructure.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = [];
    public string[] AllowedHeaders { get; set; } = [];
    public bool AllowCredentials { get; set; }
}
