namespace RRMS.Infrastructure.Options;

public class ExportOptions
{
    public const string SectionName = "Export";
    public int MaxQueueItems { get; init; }
}