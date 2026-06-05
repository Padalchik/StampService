namespace StampService.Contracts.DTOs.Audit;

public record BusinessAuditLogsResponse(
    IReadOnlyCollection<BusinessAuditLogResponse> Items,
    int TotalCount,
    int Take);
