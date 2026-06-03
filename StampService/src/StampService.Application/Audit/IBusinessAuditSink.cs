namespace StampService.Application.Audit;

public interface IBusinessAuditSink
{
    Task RecordAsync(BusinessAuditEvent auditEvent, CancellationToken cancellationToken);
}
