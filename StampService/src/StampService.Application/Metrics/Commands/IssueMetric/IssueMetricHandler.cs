using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Metrics;
using StampService.Domain.Access;

namespace StampService.Application.Metrics.Commands.IssueMetric;

public class IssueMetricHandler : ICommandHandler<IssueMetricResponse, IssueMetricCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandRepository _brandRepository;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IUserRepository _userRepository;

    public IssueMetricHandler(
        IBrandAccessService brandAccessService,
        IBrandRepository brandRepository,
        IMetricLedgerService metricLedgerService,
        ILoyaltyMetricRepository metricRepository,
        IUserRepository userRepository)
    {
        _brandAccessService = brandAccessService;
        _brandRepository = brandRepository;
        _metricLedgerService = metricLedgerService;
        _metricRepository = metricRepository;
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

        var ledgerResult = await _metricLedgerService.IssueAsync(
            command.Request.UserId,
            metric.BrandId,
            command.MetricDefinitionId,
            command.Request.Amount,
            command.Request.Comment,
            cancellationToken);

        if (ledgerResult.IsFailed)
            return Result.Fail(ledgerResult.Errors);

        var balance = ledgerResult.Value.Balance;
        var transaction = ledgerResult.Value.Transaction;

        var response = new IssueMetricResponse(
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
