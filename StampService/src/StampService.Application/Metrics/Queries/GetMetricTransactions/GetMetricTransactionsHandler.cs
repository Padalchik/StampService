using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Queries.GetMetricTransactions;

public class GetMetricTransactionsHandler : IQueryHandler<MetricTransactionsResponse, GetMetricTransactionsQuery>
{
    private const int MaxTake = 100;

    private readonly IBrandAccessService _brandAccessService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly IUserRepository _userRepository;

    public GetMetricTransactionsHandler(
        IBrandAccessService brandAccessService,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _stampTransactionRepository = stampTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<MetricTransactionsResponse>> Handle(
        GetMetricTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Skip < 0)
            return Result.Fail("Skip cannot be negative");

        if (query.Take <= 0 || query.Take > MaxTake)
            return Result.Fail($"Take must be between 1 and {MaxTake}");

        var metric = await _metricRepository.GetByIdAsync(
            query.MetricDefinitionId,
            cancellationToken);

        if (metric is null)
            return Result.Fail("Metric not found");

        var canViewBalance = await _brandAccessService.CanAsync(
            query.RequestUserId,
            metric.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalance)
            return Result.Fail("Access denied");

        var userExists = await _userRepository.ExistsAsync(query.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail("User not found");

        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            query.UserId,
            metric.BrandId,
            query.MetricDefinitionId,
            cancellationToken);

        if (balance is null)
            return Result.Ok(new MetricTransactionsResponse(
                metric.BrandId,
                query.MetricDefinitionId,
                query.UserId,
                query.Skip,
                query.Take,
                []));

        var transactions = await _stampTransactionRepository.GetHistoryByMetricBalanceAsync(
            balance.Id,
            query.Skip,
            query.Take,
            cancellationToken);

        var items = transactions
            .Select(transaction => new MetricTransactionResponse(
                transaction.Id,
                balance.Id,
                balance.MetricDefinitionId,
                balance.UserId,
                transaction.Amount,
                transaction.Comment,
                transaction.CreatedAt))
            .ToArray();

        return Result.Ok(new MetricTransactionsResponse(
            metric.BrandId,
            query.MetricDefinitionId,
            query.UserId,
            query.Skip,
            query.Take,
            items));
    }
}
