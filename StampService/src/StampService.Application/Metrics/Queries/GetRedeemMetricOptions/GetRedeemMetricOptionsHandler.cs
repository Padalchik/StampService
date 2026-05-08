using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using DomainRedemptionCode = StampService.Domain.User.RedemptionCode;

namespace StampService.Application.Metrics.Queries.GetRedeemMetricOptions;

public class GetRedeemMetricOptionsHandler : IQueryHandler<RedeemMetricOptionsResponse, GetRedeemMetricOptionsQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IRedemptionCodeRepository _redemptionCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _timeProvider;

    public GetRedeemMetricOptionsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IRedemptionCodeRepository redemptionCodeRepository,
        IUserRepository userRepository,
        TimeProvider timeProvider)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _redemptionCodeRepository = redemptionCodeRepository;
        _userRepository = userRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RedeemMetricOptionsResponse>> Handle(
        GetRedeemMetricOptionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.RedeemerUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canRedeem = await _brandAccessService.CanAsync(
            query.RedeemerUserId,
            query.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
            return Result.Fail(AccessErrors.Denied());

        var code = query.RedemptionCode.Trim();
        if (!DomainRedemptionCode.IsValidCode(code))
            return Result.Fail(UserErrors.RedemptionCodeInvalid());

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var activeCode = await _redemptionCodeRepository.GetActiveByCodeAsync(
            code,
            nowUtc,
            cancellationToken);

        if (activeCode is null)
            return Result.Fail(UserErrors.RedemptionCodeNotFoundOrExpired());

        var customer = await _userRepository.GetByIdAsync(activeCode.UserId, cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.NotFound());

        var metrics = await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        var activeMetrics = metrics
            .Where(metric => metric.IsActive)
            .OrderBy(metric => metric.Name)
            .ToArray();

        var responseItems = new List<RedeemMetricOptionResponse>(activeMetrics.Length);
        foreach (var metric in activeMetrics)
        {
            var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
                customer.Id,
                metric.BrandId,
                metric.Id,
                cancellationToken);

            var currentBalance = balance?.Value ?? 0;
            responseItems.Add(new RedeemMetricOptionResponse(
                metric.Id,
                metric.Name,
                metric.Code,
                currentBalance,
                metric.RedemptionAmount,
                currentBalance >= metric.RedemptionAmount));
        }

        return Result.Ok(new RedeemMetricOptionsResponse(
            customer.Id,
            customer.Name,
            code,
            responseItems));
    }
}
