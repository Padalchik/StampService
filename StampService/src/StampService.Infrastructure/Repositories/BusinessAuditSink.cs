using System.Text.Json;
using Microsoft.Extensions.Logging;
using StampService.Application.Audit;
using StampService.Domain.Audit;

namespace StampService.Infrastructure.Repositories;

public sealed class BusinessAuditSink : IBusinessAuditSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBusinessAuditContext _auditContext;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BusinessAuditSink> _logger;
    private readonly TimeProvider _timeProvider;

    public BusinessAuditSink(
        AppDbContext dbContext,
        IBusinessAuditContext auditContext,
        TimeProvider timeProvider,
        ILogger<BusinessAuditSink> logger)
    {
        _auditContext = auditContext;
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task RecordAsync(BusinessAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var metadataJson = auditEvent.Metadata is null || auditEvent.Metadata.Count == 0
            ? null
            : JsonSerializer.Serialize(auditEvent.Metadata, JsonOptions);

        var logResult = BusinessAuditLog.Create(
            _timeProvider.GetUtcNow().UtcDateTime,
            auditEvent.OperationType,
            auditEvent.OperationStatus,
            _auditContext.Channel,
            auditEvent.BrandId,
            auditEvent.ActorUserId,
            auditEvent.CustomerUserId,
            auditEvent.TargetEntityType,
            auditEvent.TargetEntityId,
            auditEvent.Amount,
            auditEvent.BalanceBefore,
            auditEvent.BalanceAfter,
            auditEvent.ReasonCode,
            auditEvent.Comment,
            _auditContext.TraceId,
            metadataJson);

        if (logResult.IsFailed)
        {
            _logger.LogWarning(
                "Business audit event was skipped because payload is invalid. OperationType={OperationType} OperationStatus={OperationStatus} Reason={Reason}",
                auditEvent.OperationType,
                auditEvent.OperationStatus,
                BusinessAuditReason.FromErrors(logResult.Errors));
            return;
        }

        var log = logResult.Value;

        try
        {
            _dbContext.BusinessAuditLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _dbContext.Entry(log).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
            _logger.LogError(
                ex,
                "Business audit event persistence failed. OperationType={OperationType} OperationStatus={OperationStatus} BrandId={BrandId} ActorUserId={ActorUserId} CustomerUserId={CustomerUserId}",
                auditEvent.OperationType,
                auditEvent.OperationStatus,
                auditEvent.BrandId,
                auditEvent.ActorUserId,
                auditEvent.CustomerUserId);
        }
    }
}
