using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.User;

namespace StampService.Application.Brands.Commands.AddBrandStaffByPhone;

public class AddBrandStaffByPhoneHandler
    : ICommandHandler<AddBrandStaffByPhoneResponse, AddBrandStaffByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandMembershipService _brandMembershipService;
    private readonly IBusinessAuditSink _businessAuditSink;
    private readonly IUserRepository _userRepository;

    public AddBrandStaffByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandMembershipService brandMembershipService,
        IUserRepository userRepository,
        IBusinessAuditSink? businessAuditSink = null)
    {
        _brandAccessService = brandAccessService;
        _brandMembershipService = brandMembershipService;
        _businessAuditSink = businessAuditSink ?? NoopBusinessAuditSink.Instance;
        _userRepository = userRepository;
    }

    public async Task<Result<AddBrandStaffByPhoneResponse>> Handle(
        AddBrandStaffByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        var canManageStaff = await BrandStaffAuthorization.CanManageStaffAsync(
            _brandAccessService,
            command.ActorUserId,
            command.BrandId,
            cancellationToken);

        if (!canManageStaff)
            return await RejectedAsync(command, [AccessErrors.Denied()], null, null, cancellationToken);

        var phoneNumberResult = PhoneNumberNormalizer.NormalizeForAuth(
            command.PhoneNumber,
            nameof(command.PhoneNumber));
        if (phoneNumberResult.IsFailed)
            return await RejectedAsync(command, phoneNumberResult.Errors, null, null, cancellationToken);

        var phoneNumber = phoneNumberResult.Value;
        var user = await _userRepository.GetByIdentityAsync(
            IdentityType.Phone,
            phoneNumber,
            cancellationToken);
        if (user is null)
            return await RejectedAsync(command, [UserErrors.RecipientNotFound()], null, null, cancellationToken);

        var membershipResult = await _brandMembershipService.AddStaffAsync(
            command.BrandId,
            user.Id,
            cancellationToken);

        if (membershipResult.IsFailed)
            return await RejectedAsync(command, membershipResult.Errors, user.Id, null, cancellationToken);

        var membership = membershipResult.Value;
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.AddStaff,
                BusinessAuditOperationStatus.Succeeded,
                BrandId: command.BrandId,
                ActorUserId: command.ActorUserId,
                CustomerUserId: user.Id,
                TargetEntityType: BusinessAuditTargetEntityType.BrandMembership,
                TargetEntityId: membership.Id),
            cancellationToken);

        return Result.Ok(new AddBrandStaffByPhoneResponse(
            membership.BrandId,
            user.Id,
            user.Name,
            phoneNumber,
            membership.Id,
            membership.CreatedAt));
    }

    private async Task<Result<AddBrandStaffByPhoneResponse>> RejectedAsync(
        AddBrandStaffByPhoneCommand command,
        IReadOnlyCollection<IError> errors,
        Guid? targetUserId,
        Guid? targetEntityId,
        CancellationToken cancellationToken)
    {
        await _businessAuditSink.RecordAsync(
            new BusinessAuditEvent(
                BusinessAuditOperationType.AddStaff,
                BusinessAuditOperationStatus.Rejected,
                BrandId: command.BrandId,
                ActorUserId: command.ActorUserId,
                CustomerUserId: targetUserId,
                TargetEntityType: BusinessAuditTargetEntityType.BrandMembership,
                TargetEntityId: targetEntityId,
                ReasonCode: BusinessAuditReason.FromErrors(errors)),
            cancellationToken);

        return Result.Fail(errors);
    }
}
