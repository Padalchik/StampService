namespace StampService.Application.Audit;

public sealed class NoopBusinessAuditSink : IBusinessAuditSink
{
    public static NoopBusinessAuditSink Instance { get; } = new();

    private NoopBusinessAuditSink()
    {
    }

    public Task RecordAsync(BusinessAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
