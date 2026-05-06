using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;
using StampService.Domain.Loyalty;

namespace StampService.Application.Metrics.Commands.RedeemMetric;

public class RedeemMetricHandler : ICommandHandler<RedeemMetricResponse, RedeemMetricCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IMetricBalanceRepository _metricBalanceRepository;
    private readonly IStampTransactionRepository _stampTransactionRepository;
    private readonly IUserRepository _userRepository;

    public RedeemMetricHandler(
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

    public async Task<Result<RedeemMetricResponse>> Handle(
        RedeemMetricCommand command,
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

        var canRedeem = await _brandAccessService.CanAsync(
            command.RedeemerUserId,
            metric.BrandId,
            PermissionCode.StampRedeem,
            cancellationToken);

        if (!canRedeem)
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
            return Result.Fail("Metric balance not found");

        var subtractResult = balance.Subtract(command.Request.Amount);
        if (subtractResult.IsFailed)
            return Result.Fail(subtractResult.Errors);

        var transactionResult = StampTransaction.CreateRedeem(
            balance.Id,
            command.Request.Amount,
            command.Request.Comment);

        if (transactionResult.IsFailed)
            return Result.Fail(transactionResult.Errors);

        var transaction = transactionResult.Value;
        _stampTransactionRepository.Add(transaction);
        await _stampTransactionRepository.SaveAsync(cancellationToken);

        var response = new RedeemMetricResponse(
            transaction.Id,
            balance.Id,
            balance.BrandId,
            balance.MetricDefinitionId,
            balance.UserId,
            transaction.Type.ToString(),
            transaction.Amount,
            balance.Value,
            transaction.CreatedAt);

        return Result.Ok(response);
    }
}
