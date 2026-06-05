using System.Diagnostics;
using StampService.Application.Audit;

namespace StampService.TelegramBot.Common.Audit;

public sealed class TelegramBusinessAuditContext : IBusinessAuditContext
{
    public string Channel => "Telegram";

    public string? TraceId => Activity.Current?.TraceId.ToString();
}
