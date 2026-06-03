namespace StampService.Application.Audit;

public record BusinessAuditLogFilter(
    DateTime? OccurredFromUtc,
    DateTime? OccurredToUtc,
    Guid? BrandId,
    Guid? CustomerUserId,
    string? ActorName,
    string? OperationType,
    string? OperationStatus,
    int Take);
