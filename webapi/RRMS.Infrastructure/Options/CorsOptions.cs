namespace RRMS.Infrastructure.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
    public string[] AllowedHeaders { get; set; } = ["Authorization", "Content-Type", "X-XSRF-TOKEN", "X-Client-Action-Id", "X-Client-Request-Id", "X-Request-Id"];
    public bool AllowCredentials { get; set; } = true;
}
