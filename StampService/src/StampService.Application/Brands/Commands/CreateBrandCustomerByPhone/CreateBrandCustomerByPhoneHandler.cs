using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Brands.Queries.GetBrandCustomerCard;
using StampService.Application.Coins;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Contracts.DTOs.Brands;
using StampService.Domain.Access;

namespace StampService.Application.Brands.Commands.CreateBrandCustomerByPhone;

public class CreateBrandCustomerByPhoneHandler
    : ICommandHandler<BrandCustomerCardResponse, CreateBrandCustomerByPhoneCommand>
{
    private readonly IBrandAccessService _brandAccessService;
    private readonly IBrandCustomerRepository _brandCustomerRepository;
    private readonly IBrandCustomerService _brandCustomerService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinLedgerService _coinLedgerService;
    private readonly IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery> _customerCardHandler;
    private readonly IMetricLedgerService _metricLedgerService;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IPhoneAccountService _phoneAccountService;

    public CreateBrandCustomerByPhoneHandler(
        IBrandAccessService brandAccessService,
        IBrandCustomerRepository brandCustomerRepository,
        IBrandCustomerService brandCustomerService,
        IBrandRepository brandRepository,
        IMetricLedgerService metricLedgerService,
        ICoinLedgerService coinLedgerService,
        ILoyaltyMetricRepository metricRepository,
        IPhoneAccountService phoneAccountService,
        IQueryHandler<BrandCustomerCardResponse, GetBrandCustomerCardQuery> customerCardHandler)
    {
        _brandAccessService = brandAccessService;
        _brandCustomerRepository = brandCustomerRepository;
        _brandCustomerService = brandCustomerService;
        _brandRepository = brandRepository;
        _coinLedgerService = coinLedgerService;
        _customerCardHandler = customerCardHandler;
        _metricLedgerService = metricLedgerService;
        _metricRepository = metricRepository;
        _phoneAccountService = phoneAccountService;
    }

    public async Task<Result<BrandCustomerCardResponse>> Handle(
        CreateBrandCustomerByPhoneCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ActorUserId == Guid.Empty)
            return Result.Fail(UserErrors.IdIsEmpty());

        if (command.BrandId == Guid.Empty)
            return Result.Fail(BrandErrors.IdIsEmpty());

        var canViewBalances = await _brandAccessService.CanAsync(
            command.ActorUserId,
            command.BrandId,
            PermissionCode.BalanceView,
            cancellationToken);

        if (!canViewBalances)
            return Result.Fail(AccessErrors.Denied());

        var brand = await _brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return Result.Fail(BrandErrors.NotFound());

        var customerResult = await _phoneAccountService.GetOrCreateForBusinessOperationWithStatusAsync(
            command.Request.PhoneNumber,
            nameof(command.Request.PhoneNumber),
            cancellationToken);
        if (customerResult.IsFailed)
            return Result.Fail(customerResult.Errors);

        var customer = customerResult.Value.User;
        var customerLinkResult = await _brandCustomerService.EnsureAsync(
            brand.Id,
            customer.Id,
            command.ActorUserId,
            cancellationToken);
        if (customerLinkResult.IsFailed)
            return Result.Fail(customerLinkResult.Errors);

        if (customerLinkResult.Value && brand.IsWelcomeRewardsEnabled)
        {
            var welcomeResult = await IssueWelcomeRewardsAsync(
                brand,
                customer.Id,
                command.ActorUserId,
                cancellationToken);
            if (welcomeResult.IsFailed)
                return Result.Fail(welcomeResult.Errors);
        }
        else
        {
            if (customerResult.Value.Created || customerLinkResult.Value)
                await _brandCustomerRepository.SaveAsync(cancellationToken);
        }

        return await _customerCardHandler.Handle(
            new GetBrandCustomerCardQuery(
                command.ActorUserId,
                command.BrandId,
                command.Request.PhoneNumber),
            cancellationToken);
    }

    private async Task<Result> IssueWelcomeRewardsAsync(
        Domain.Brand.Brand brand,
        Guid customerUserId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var issuedAnyReward = false;
        var comment = string.IsNullOrWhiteSpace(brand.WelcomeRewardComment)
            ? "Приветственная награда"
            : brand.WelcomeRewardComment;

        if (brand.IsMetricsEnabled && brand.WelcomeMetricRewards.Count > 0)
        {
            var configuredMetricIds = brand.WelcomeMetricRewards
                .Select(reward => reward.MetricDefinitionId)
                .ToHashSet();
            var metrics = await _metricRepository.GetByBrandAsync(brand.Id, cancellationToken);
            var activeMetricIds = metrics
                .Where(metric => metric.IsActive && configuredMetricIds.Contains(metric.Id))
                .Select(metric => metric.Id)
                .ToHashSet();

            foreach (var welcomeMetric in brand.WelcomeMetricRewards.Where(reward => activeMetricIds.Contains(reward.MetricDefinitionId)))
            {
                var issueResult = await _metricLedgerService.IssueAsync(
                    customerUserId,
                    actorUserId,
                    brand.Id,
                    welcomeMetric.MetricDefinitionId,
                    welcomeMetric.Amount,
                    comment,
                    cancellationToken);
                if (issueResult.IsFailed)
                    return Result.Fail(issueResult.Errors);

                issuedAnyReward = true;
            }
        }

        if (brand.IsCoinsEnabled && brand.WelcomeCoinsAmount > 0)
        {
            var issueResult = await _coinLedgerService.IssueAsync(
                customerUserId,
                actorUserId,
                brand.Id,
                brand.WelcomeCoinsAmount,
                comment,
                cancellationToken);
            if (issueResult.IsFailed)
                return Result.Fail(issueResult.Errors);

            issuedAnyReward = true;
        }

        if (!issuedAnyReward)
            await _brandCustomerRepository.SaveAsync(cancellationToken);

        return Result.Ok();
    }
}
