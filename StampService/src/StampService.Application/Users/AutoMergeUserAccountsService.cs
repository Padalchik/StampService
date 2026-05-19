using FluentResults;
using StampService.Application.Access;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class AutoMergeUserAccountsService : IAutoMergeUserAccountsService
{
    private readonly IBrandMembershipRepository _brandMembershipRepository;
    private readonly ICoinWalletRepository _coinWalletRepository;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IUserRepository _userRepository;

    public AutoMergeUserAccountsService(
        IBrandMembershipRepository brandMembershipRepository,
        ICoinWalletRepository coinWalletRepository,
        ICoinLedgerService coinLedgerService,
        IMetricBalanceRepository metricBalanceRepository,
        IMetricLedgerService metricLedgerService,
        IRedemptionCodeRepository redemptionCodeRepository,
        IUserRepository userRepository)
    {
        _brandMembershipRepository = brandMembershipRepository;
        _coinWalletRepository = coinWalletRepository;
        _coinLedgerService = coinLedgerService;
        _metricBalanceRepository = metricBalanceRepository;
        _metricLedgerService = metricLedgerService;
        _redemptionCodeRepository = redemptionCodeRepository;
        _userRepository = userRepository;
    }

    public async Task<Result> MergeSingleIdentitySourceIntoTargetAsync(
        User targetUser,
        User sourceUser,
        IdentityType identityType,
        string identityKey,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var activeSourceIdentities = sourceUser.Identities
            .Where(identity => identity.DeletedAt is null)
            .ToArray();
        var sourceIdentity = activeSourceIdentities.SingleOrDefault(identity =>
            identity.Type == identityType && identity.Key == identityKey);

        if (sourceIdentity is null)
            return Result.Fail(UserErrors.IdentityMergeNotAllowed());

        if (activeSourceIdentities.Length != 1)
            return Result.Fail(UserErrors.IdentityMergeSourceHasMultipleIdentities());

        var targetIdentity = targetUser.Identities.FirstOrDefault(identity =>
            identity.DeletedAt is null && identity.Type == identityType);

        var sourceMemberships = await _brandMembershipRepository.GetUserMembershipsAsync(
            sourceUser.Id,
            cancellationToken);
        foreach (var membership in sourceMemberships)
        {
            var targetMembership = await _brandMembershipRepository.GetByBrandAndUserAsync(
                membership.BrandId,
                targetUser.Id,
                cancellationToken);
            if (targetMembership is not null)
                return Result.Fail(UserErrors.IdentityMergeTargetHasBrandMembership());
        }

        if (targetIdentity is not null && targetIdentity.Key != sourceIdentity.Key)
            targetIdentity.Deactivate(nowUtc);

        if (targetIdentity is null || targetIdentity.Key != sourceIdentity.Key)
        {
            var reassignResult = sourceIdentity.ReassignTo(targetUser);
            if (reassignResult.IsFailed)
                return Result.Fail(reassignResult.Errors);
        }

        var coinWallets = await _coinWalletRepository.GetUserWalletsAsync(sourceUser.Id, cancellationToken);
        var metricBalances = await _metricBalanceRepository.GetUserBalancesAsync(sourceUser.Id, cancellationToken);

        var activeRedemptionCode = await _redemptionCodeRepository.GetActiveByUserIdAsync(
            sourceUser.Id,
            nowUtc,
            cancellationToken);
        if (activeRedemptionCode is not null)
        {
            var expireResult = activeRedemptionCode.Expire(nowUtc);
            if (expireResult.IsFailed)
                return Result.Fail(expireResult.Errors);
        }

        foreach (var membership in sourceMemberships)
        {
            var reassignResult = membership.ReassignToUser(targetUser.Id);
            if (reassignResult.IsFailed)
                return Result.Fail(reassignResult.Errors);
        }

        sourceUser.Deactivate(nowUtc);

        foreach (var wallet in coinWallets.Where(wallet => wallet.Value > 0))
        {
            var result = await _coinLedgerService.IssueAsync(
                targetUser.Id,
                targetUser.Id,
                wallet.BrandId,
                wallet.Value,
                BuildMergeComment(sourceUser.CustomerCode),
                cancellationToken);
            if (result.IsFailed)
                return Result.Fail(result.Errors);
        }

        foreach (var balance in metricBalances.Where(balance => balance.Value > 0))
        {
            var result = await _metricLedgerService.IssueAsync(
                targetUser.Id,
                targetUser.Id,
                balance.BrandId,
                balance.MetricDefinitionId,
                balance.Value,
                BuildMergeComment(sourceUser.CustomerCode),
                cancellationToken);
            if (result.IsFailed)
                return Result.Fail(result.Errors);
        }

        await _userRepository.SaveAsync(cancellationToken);

        return Result.Ok();
    }

    private static string BuildMergeComment(string sourceCustomerCode)
    {
        return $"Перенос при объединении аккаунтов. Исходный код клиента: {sourceCustomerCode}.";
    }
}
