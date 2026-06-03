using System.Diagnostics;

namespace StampService.Application.Audit;

public sealed class DefaultBusinessAuditContext : IBusinessAuditContext
{
    public string Channel => "Application";

    public string? TraceId => Activity.Current?.TraceId.ToString();
}
