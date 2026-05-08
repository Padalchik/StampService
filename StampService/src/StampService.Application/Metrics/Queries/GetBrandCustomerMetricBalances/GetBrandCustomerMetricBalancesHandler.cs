using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Errors;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using UserEntity = StampService.Domain.User.User;

namespace StampService.Application.Metrics.Queries.GetBrandCustomerMetricBalances;

public class GetBrandCustomerMetricBalancesHandler
    : IQueryHandler<BrandCustomerMetricBalancesResponse, GetBrandCustomerMetricBalancesQuery>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IUserRepository _userRepository;

    public GetBrandCustomerMetricBalancesHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<BrandCustomerMetricBalancesResponse>> Handle(
        GetBrandCustomerMetricBalancesQuery query,
        CancellationToken cancellationToken)
    {
        if (query.RequestUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (query.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canViewBalances = await _brandAccessService.CanAsync(
            query.RequestUserId,
            query.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalances)
            return Result.Fail(AccessErrors.Denied());

        var customerCode = query.CustomerCode.Trim();
        if (!UserEntity.IsValidCustomerCode(customerCode))
            return Result.Fail(UserErrors.CustomerCodeInvalid());

        var customer = await _userRepository.GetByCustomerCodeAsync(customerCode, cancellationToken);
        if (customer is null)
            return Result.Fail(UserErrors.RecipientNotFound());

        var metrics = await _metricRepository.GetByBrandAsync(query.BrandId, cancellationToken);
        var balances = new List<BrandCustomerMetricBalanceResponse>(metrics.Count);

        foreach (var metric in metrics.OrderBy(metric => metric.Name))
        {
            var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
                customer.Id,
                query.BrandId,
                metric.Id,
                cancellationToken);

            balances.Add(new BrandCustomerMetricBalanceResponse(
                metric.Id,
                metric.Code,
                metric.Name,
                balance?.Value ?? 0,
                metric.IsActive));
        }

        return Result.Ok(new BrandCustomerMetricBalancesResponse(
            query.BrandId,
            customer.Id,
            customer.Name,
            customer.CustomerCode,
            balances));
    }
}
