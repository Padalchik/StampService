namespace StampService.Application.Audit;

public interface IBusinessAuditLogRepository
{
    Task<BusinessAuditLogPage> GetAsync(
        BusinessAuditLogFilter filter,
        CancellationToken cancellationToken);
}
