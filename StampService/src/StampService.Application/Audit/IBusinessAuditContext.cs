namespace StampService.Application.Audit;

public interface IBusinessAuditContext
{
    string Channel { get; }
    string? TraceId { get; }
}
