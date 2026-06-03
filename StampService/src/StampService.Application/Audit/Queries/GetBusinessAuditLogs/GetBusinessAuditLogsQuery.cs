using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Audit.Queries.GetBusinessAuditLogs;

public record GetBusinessAuditLogsQuery(
    AdminActor Admin,
    DateTime? OccurredFromUtc = null,
    DateTime? OccurredToUtc = null,
    Guid? BrandId = null,
    string? CustomerPhoneNumber = null,
    string? ActorName = null,
    string? OperationType = null,
    string? OperationStatus = null,
    int Take = 50) : IQuery;
