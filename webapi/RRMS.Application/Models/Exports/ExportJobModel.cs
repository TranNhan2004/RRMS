namespace RRMS.Application.Models.Exports;

public class ExportJobModel
{
    public required Guid JobId { get; init; }
    public required Guid UserId { get; init; }
    public required string FileName { get; init; }
    public required object Data { get; init; }
    public TimeZoneInfo TimeZoneId { get; init; } = TimeZoneInfo.Utc;
    public DateTimeOffset? ExpiredAt { get; init; }
}
