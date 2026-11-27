namespace RRMS.Application.Interfaces.Contexts;

public interface ICurrentRequestContext
{
    string? RemoteIpAddress { get; }
    string? UserAgent { get; }
    string? TraceId { get; }
    string? ClientActionId { get; }
    string? ClientRequestId { get; }
}
