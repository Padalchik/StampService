namespace StampService.Application.Audit;

public record BusinessAuditEvent(
    string OperationType,
    string OperationStatus,
    Guid? BrandId = null,
    Guid? ActorUserId = null,
    Guid? CustomerUserId = null,
    string? TargetEntityType = null,
    Guid? TargetEntityId = null,
    int? Amount = null,
    int? BalanceBefore = null,
    int? BalanceAfter = null,
    string? ReasonCode = null,
    string? Comment = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);
