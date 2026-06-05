using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Auth;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Audit;
using StampService.Domain.User;

namespace StampService.Application.Audit.Queries.GetBusinessAuditLogs;

public class GetBusinessAuditLogsHandler : IQueryHandler<BusinessAuditLogsResponse, GetBusinessAuditLogsQuery>
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;

    private readonly IAdminAccessService _adminAccessService;
    private readonly IBusinessAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;

    public GetBusinessAuditLogsHandler(
        IAdminAccessService adminAccessService,
        IBusinessAuditLogRepository auditLogRepository,
        IUserRepository userRepository)
    {
        _adminAccessService = adminAccessService;
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<BusinessAuditLogsResponse>> Handle(
        GetBusinessAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(query.Admin, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        if (query.OccurredFromUtc is { } from && query.OccurredToUtc is { } to && from > to)
        {
            return Result.Fail(AppError.Validation(
                AppErrorCodes.Validation.ValueInvalid,
                "Period start cannot be later than period end",
                nameof(query.OccurredFromUtc)));
        }

        var take = query.Take <= 0 ? DefaultTake : Math.Min(query.Take, MaxTake);

        Guid? customerUserId = null;
        if (!string.IsNullOrWhiteSpace(query.CustomerPhoneNumber))
        {
            var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
                query.CustomerPhoneNumber,
                nameof(query.CustomerPhoneNumber));
            if (phoneNumberResult.IsFailed)
                return Result.Fail(phoneNumberResult.Errors);

            var user = await _userRepository.GetByIdentityAsync(
                IdentityType.Phone,
                phoneNumberResult.Value,
                cancellationToken);

            customerUserId = user?.Id ?? Guid.Empty;
        }

        var page = await _auditLogRepository.GetAsync(
            new BusinessAuditLogFilter(
                query.OccurredFromUtc,
                query.OccurredToUtc,
                query.BrandId,
                customerUserId,
                Normalize(query.ActorName),
                Normalize(query.OperationType),
                Normalize(query.OperationStatus),
                take),
            cancellationToken);

        var response = new BusinessAuditLogsResponse(
            page.Items.Select(ToResponse).ToArray(),
            page.TotalCount,
            take);

        return Result.Ok(response);
    }

    private static BusinessAuditLogResponse ToResponse(BusinessAuditLogReadModel log)
    {
        var operationName = GetOperationName(log.OperationType);
        var statusText = GetStatusText(log.OperationStatus);
        var summary = CreateSummary(log, operationName, statusText);

        return new BusinessAuditLogResponse(
            log.OccurredAt,
            log.OperationType,
            operationName,
            log.OperationStatus,
            statusText,
            GetChannelName(log.Channel),
            log.BrandName,
            log.ActorName,
            log.CustomerName,
            log.TargetEntityType,
            log.Amount,
            log.BalanceBefore,
            log.BalanceAfter,
            log.ReasonCode,
            log.Comment,
            summary);
    }

    private static string CreateSummary(
        BusinessAuditLogReadModel log,
        string operationName,
        string statusText)
    {
        var actor = string.IsNullOrWhiteSpace(log.ActorName) ? "Исполнитель" : log.ActorName;
        var customer = string.IsNullOrWhiteSpace(log.CustomerName) ? "клиент" : log.CustomerName;
        var amount = FormatAmount(log.OperationType, log.Amount);
        var brand = string.IsNullOrWhiteSpace(log.BrandName) ? null : $" · {log.BrandName}";
        var statusSuffix = log.OperationStatus == BusinessAuditOperationStatus.Succeeded
            ? string.Empty
            : $" · {statusText}";

        return log.OperationType switch
        {
            BusinessAuditOperationType.IssueCoins => $"{actor} начислил {customer} {amount} монет{brand}{statusSuffix}",
            BusinessAuditOperationType.RedeemCoins => $"{actor} списал у {customer} {amount} монет{brand}{statusSuffix}",
            BusinessAuditOperationType.IssueMetric => $"{actor} начислил {customer} {amount} по метрике{brand}{statusSuffix}",
            BusinessAuditOperationType.RedeemMetric => $"{actor} списал у {customer} метрику{brand}{statusSuffix}",
            BusinessAuditOperationType.PurchaseCoinProduct => $"{actor} выдал товар {customer}{brand}{statusSuffix}",
            BusinessAuditOperationType.AddStaff => $"{actor} добавил сотрудника{brand}{statusSuffix}",
            BusinessAuditOperationType.UpdateRewardSettings => $"{actor} изменил настройки наград{brand}{statusSuffix}",
            _ => $"{operationName}{brand}{statusSuffix}"
        };
    }

    private static string FormatAmount(string operationType, int? amount)
    {
        if (amount is null)
            return string.Empty;

        var sign = operationType is BusinessAuditOperationType.IssueCoins or BusinessAuditOperationType.IssueMetric
            ? "+"
            : "-";
        return $"{sign}{amount.Value}";
    }

    private static string GetOperationName(string operationType)
    {
        return operationType switch
        {
            BusinessAuditOperationType.IssueCoins => "Начисление монет",
            BusinessAuditOperationType.RedeemCoins => "Списание монет",
            BusinessAuditOperationType.IssueMetric => "Начисление метрики",
            BusinessAuditOperationType.RedeemMetric => "Списание метрики",
            BusinessAuditOperationType.PurchaseCoinProduct => "Выдача товара",
            BusinessAuditOperationType.AddStaff => "Добавление сотрудника",
            BusinessAuditOperationType.UpdateRewardSettings => "Настройки наград",
            _ => operationType
        };
    }

    private static string GetStatusText(string operationStatus)
    {
        return operationStatus switch
        {
            BusinessAuditOperationStatus.Succeeded => "Выполнено",
            BusinessAuditOperationStatus.Rejected => "Отклонено",
            BusinessAuditOperationStatus.Failed => "Ошибка",
            _ => operationStatus
        };
    }

    private static string GetChannelName(string channel)
    {
        return channel switch
        {
            "Web" => "Web",
            "Telegram" => "Telegram",
            "Application" => "Система",
            _ => channel
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
