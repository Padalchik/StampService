using FluentResults;
using StampService.Domain.Shared;

namespace StampService.Domain.Audit;

public class BusinessAuditLog : BaseEntity
{
    public DateTime OccurredAt { get; private set; }
    public string OperationType { get; private set; } = null!;
    public string OperationStatus { get; private set; } = null!;
    public string Channel { get; private set; } = null!;
    public Guid? BrandId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid? CustomerUserId { get; private set; }
    public string? TargetEntityType { get; private set; }
    public Guid? TargetEntityId { get; private set; }
    public int? Amount { get; private set; }
    public int? BalanceBefore { get; private set; }
    public int? BalanceAfter { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Comment { get; private set; }
    public string? TraceId { get; private set; }
    public string? MetadataJson { get; private set; }

    private BusinessAuditLog(
        DateTime occurredAt,
        string operationType,
        string operationStatus,
        string channel,
        Guid? brandId,
        Guid? actorUserId,
        Guid? customerUserId,
        string? targetEntityType,
        Guid? targetEntityId,
        int? amount,
        int? balanceBefore,
        int? balanceAfter,
        string? reasonCode,
        string? comment,
        string? traceId,
        string? metadataJson)
    {
        OccurredAt = occurredAt;
        OperationType = operationType;
        OperationStatus = operationStatus;
        Channel = channel;
        BrandId = brandId;
        ActorUserId = actorUserId;
        CustomerUserId = customerUserId;
        TargetEntityType = targetEntityType;
        TargetEntityId = targetEntityId;
        Amount = amount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        ReasonCode = reasonCode;
        Comment = comment;
        TraceId = traceId;
        MetadataJson = metadataJson;
    }

    protected BusinessAuditLog()
    {
    }

    public static Result<BusinessAuditLog> Create(
        DateTime occurredAt,
        string operationType,
        string operationStatus,
        string channel,
        Guid? brandId = null,
        Guid? actorUserId = null,
        Guid? customerUserId = null,
        string? targetEntityType = null,
        Guid? targetEntityId = null,
        int? amount = null,
        int? balanceBefore = null,
        int? balanceAfter = null,
        string? reasonCode = null,
        string? comment = null,
        string? traceId = null,
        string? metadataJson = null)
    {
        if (string.IsNullOrWhiteSpace(operationType))
            return Result.Fail(DomainError.Validation(
                "business_audit.operation_type_required",
                "Operation type is required",
                nameof(operationType)));

        if (string.IsNullOrWhiteSpace(operationStatus))
            return Result.Fail(DomainError.Validation(
                "business_audit.operation_status_required",
                "Operation status is required",
                nameof(operationStatus)));

        if (string.IsNullOrWhiteSpace(channel))
            return Result.Fail(DomainError.Validation(
                "business_audit.channel_required",
                "Channel is required",
                nameof(channel)));

        return Result.Ok(new BusinessAuditLog(
            occurredAt,
            operationType,
            operationStatus,
            channel,
            brandId,
            actorUserId,
            customerUserId,
            Normalize(targetEntityType),
            targetEntityId,
            amount,
            balanceBefore,
            balanceAfter,
            Normalize(reasonCode),
            Normalize(comment),
            Normalize(traceId),
            Normalize(metadataJson)));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
