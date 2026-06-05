using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StampService.Application.Audit;
using StampService.Domain.Audit;

namespace StampService.Infrastructure.Repositories;

public sealed class BusinessAuditSink : IBusinessAuditSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IBusinessAuditContext _auditContext;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<BusinessAuditSink> _logger;
    private readonly TimeProvider _timeProvider;

    public BusinessAuditSink(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IBusinessAuditContext auditContext,
        TimeProvider timeProvider,
        ILogger<BusinessAuditSink> logger)
    {
        _auditContext = auditContext;
        _dbContextFactory = dbContextFactory;
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
            await using var auditDbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            auditDbContext.BusinessAuditLogs.Add(log);
            await auditDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
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
