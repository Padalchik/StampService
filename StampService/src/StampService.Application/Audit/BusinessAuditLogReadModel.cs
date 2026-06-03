namespace StampService.Application.Audit;

public record BusinessAuditLogReadModel(
    DateTime OccurredAt,
    string OperationType,
    string OperationStatus,
    string Channel,
    Guid? BrandId,
    string? BrandName,
    Guid? ActorUserId,
    string? ActorName,
    Guid? CustomerUserId,
    string? CustomerName,
    string? TargetEntityType,
    Guid? TargetEntityId,
    int? Amount,
    int? BalanceBefore,
    int? BalanceAfter,
    string? ReasonCode,
    string? Comment);
