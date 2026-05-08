using System.Net;
using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.Shared;

namespace StampService.TelegramBot.Common.Errors;

public static class BotErrorFormatter
{
    public static string Format(IReadOnlyCollection<IError> errors, BotErrorContext context = BotErrorContext.General)
    {
        var messages = errors
            .Select(error => Translate(error, context))
            .Distinct()
            .ToArray();

        return WebUtility.HtmlEncode(string.Join("; ", messages));
    }

    private static string Translate(IError error, BotErrorContext context)
    {
        var errorCode = GetErrorCode(error);
        if (errorCode == AppErrorCodes.MetricBalance.InsufficientFunds)
            return TranslateInsufficientFunds(error, context);

        return errorCode switch
        {
            AppErrorCodes.Access.AdminRequired => "нужны права администратора",
            AppErrorCodes.Access.Denied => context == BotErrorContext.IssueMetric
                ? "нет прав на выдачу метрики"
                : "нет прав на списание метрики",
            AppErrorCodes.Auth.TelegramLoginDataInvalid => "не удалось подтвердить Telegram-авторизацию",
            AppErrorCodes.Auth.UserIdClaimMissing => "не удалось определить пользователя",
            AppErrorCodes.Auth.UserIdClaimInvalid => "не удалось определить пользователя",
            AppErrorCodes.Brand.NotFound => "бренд не найден",
            AppErrorCodes.BrandMembership.NotFound => "нет доступа к этому бренду",
            AppErrorCodes.CustomerCode.Invalid => "клиентский код должен состоять из 4 цифр",
            AppErrorCodes.Metric.NotFound => "метрика не найдена",
            AppErrorCodes.Metric.Inactive => "метрика неактивна",
            AppErrorCodes.MetricBalance.NotFound => "у клиента нет баланса по этой метрике",
            AppErrorCodes.Recipient.NotFound => "получатель не найден",
            AppErrorCodes.RedemptionCode.Invalid => "код списания должен состоять из 6 цифр",
            AppErrorCodes.RedemptionCode.NotFoundOrExpired => "код списания не найден или истёк",
            AppErrorCodes.RedemptionCode.AlreadyUsed => "код списания уже использован",
            AppErrorCodes.User.NotFound => context == BotErrorContext.IssueMetric
                ? "получатель не найден"
                : "пользователь не найден",
            AppErrorCodes.Telegram.UserIdInvalid => "не удалось определить Telegram-пользователя",
            _ => error.Message
        };
    }

    private static string GetErrorCode(IError error)
    {
        return error switch
        {
            AppError appError => appError.Code,
            DomainError domainError => domainError.Code,
            _ => error.Metadata.TryGetValue("error_code", out var codeValue)
                ? codeValue?.ToString() ?? string.Empty
                : string.Empty
        };
    }

    private static string TranslateInsufficientFunds(IError error, BotErrorContext context)
    {
        var subject = context == BotErrorContext.IssueMetric
            ? "операции"
            : "списания";

        if (TryReadIntMetadata(error, "current_balance", out var current)
            && TryReadIntMetadata(error, "required_amount", out var required))
        {
            return $"недостаточно баланса для {subject} ({current}/{required})";
        }

        return $"недостаточно баланса для {subject}";
    }

    private static bool TryReadIntMetadata(IError error, string key, out int value)
    {
        value = 0;
        if (!error.Metadata.TryGetValue(key, out var raw) || raw is null)
            return false;

        if (raw is int intValue)
        {
            value = intValue;
            return true;
        }

        return int.TryParse(raw.ToString(), out value);
    }
}
