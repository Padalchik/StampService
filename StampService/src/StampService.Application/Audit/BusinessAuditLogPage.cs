namespace StampService.Application.Audit;

public record BusinessAuditLogPage(
    IReadOnlyCollection<BusinessAuditLogReadModel> Items,
    int TotalCount);
