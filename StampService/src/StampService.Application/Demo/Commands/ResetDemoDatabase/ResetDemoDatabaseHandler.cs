using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;

namespace StampService.Application.Demo.Commands.ResetDemoDatabase;

public class ResetDemoDatabaseHandler : ICommandHandler<bool, ResetDemoDatabaseCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IDemoDatabaseResetService _resetService;

    public ResetDemoDatabaseHandler(
        IAdminAccessService adminAccessService,
        IDemoDatabaseResetService resetService)
    {
        _adminAccessService = adminAccessService;
        _resetService = resetService;
    }

    public async Task<Result<bool>> Handle(
        ResetDemoDatabaseCommand command,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(command.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        await _resetService.ResetAsync(cancellationToken);
        return Result.Ok(true);
    }
}
