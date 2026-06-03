namespace StampService.Contracts.DTOs.Audit;

public record BusinessAuditLogResponse(
    DateTime OccurredAt,
    string OperationType,
    string OperationName,
    string OperationStatus,
    string OperationStatusText,
    string Channel,
    string? BrandName,
    string? ActorName,
    string? CustomerName,
    string? TargetEntityType,
    int? Amount,
    int? BalanceBefore,
    int? BalanceAfter,
    string? ReasonCode,
    string? Comment,
    string Summary);
