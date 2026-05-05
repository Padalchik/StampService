using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics.Commands.IssueMetric;

public class IssueMetricHandler : ICommandHandler<IssueMetricResponse, IssueMetricCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly IUserRepository _userRepository;

    public IssueMetricHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        ILoyaltyMetricRepository metricRepository,
        IMetricBalanceRepository metricBalanceRepository,
        IStampTransactionRepository stampTransactionRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _metricRepository = metricRepository;
        _metricBalanceRepository = metricBalanceRepository;
        _stampTransactionRepository = stampTransactionRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IssueMetricResponse>> Handle(
        IssueMetricCommand command,
        CancellationToken cancellationToken)
    {
        var metric = await _metricRepository.GetByIdAsync(
            command.MetricDefinitionId,
            cancellationToken);

        if (metric is null)
            return Result.Fail("Metric not found");

        var brandExists = await _brandRepository.ExistsAsync(metric.BrandId, cancellationToken);
        if (!brandExists)
            return Result.Fail("Brand not found");

        var canIssue = await _brandAccessService.CanAsync(
            command.IssuerUserId,
            metric.BrandId,
            PermissionCode.StampIssue,
            cancellationToken);

        if (!canIssue)
            return Result.Fail("Access denied");

        var userExists = await _userRepository.ExistsAsync(command.Request.UserId, cancellationToken);
        if (!userExists)
            return Result.Fail("User not found");

        if (!metric.IsActive)
            return Result.Fail("Metric is not active");

        var balance = await _metricBalanceRepository.GetByUserAndMetricAsync(
            command.Request.UserId,
            metric.BrandId,
            command.MetricDefinitionId,
            cancellationToken);

        if (balance is null)
        {
            var balanceResult = MetricBalance.Create(
                command.Request.UserId,
                metric.BrandId,
                command.MetricDefinitionId);

            if (balanceResult.IsFailed)
                return Result.Fail(balanceResult.Errors);

            balance = balanceResult.Value;
            _metricBalanceRepository.Add(balance);
        }

        var addResult = balance.Add(command.Request.Amount);
        if (addResult.IsFailed)
            return Result.Fail(addResult.Errors);

        var transactionResult = StampTransaction.Create(
            balance.Id,
            command.Request.Amount,
            command.Request.Comment);

        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var transaction = transactionResult.Value;
        _stampTransactionRepository.Add(transaction);
        await _stampTransactionRepository.SaveAsync(cancellationToken);

        var response = new IssueMetricResponse(
            transaction.Id,
            balance.Id,
            balance.BrandId,
            balance.MetricDefinitionId,
            balance.UserId,
            transaction.Amount,
            balance.Value,
            transaction.CreatedAt);

        return Result.Ok(response);
    }
}
