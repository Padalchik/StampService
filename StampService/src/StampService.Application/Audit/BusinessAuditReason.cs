using FluentResults;
using StampService.Application.Errors;
using StampService.Domain.Shared;

namespace StampService.Application.Audit;

public static class BusinessAuditReason
{
    public static string? FromErrors(IReadOnlyCollection<IError> errors)
    {
        var error = errors.FirstOrDefault();
        return error switch
        {
            AppError appError => appError.Code,
            DomainError domainError => domainError.Code,
            null => null,
            _ => "error.untyped"
        };
    }
}
