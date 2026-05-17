using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Brands;
using StampService.Application.CoinProducts;
using StampService.Application.Demo;
using StampService.Application.Errors;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Domain.Brand;
using StampService.Domain.Coins;
using StampService.Domain.Loyalty;
using StampService.Domain.User;

namespace StampService.Application.Demo.Commands.CreateDemoBrands;

public class CreateDemoBrandsHandler : ICommandHandler<bool, CreateDemoBrandsCommand>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandMembershipService _brandMembershipService;
    private readonly IBrandRepository _brandRepository;
    private readonly ICoinProductRepository _coinProductRepository;
    private readonly ILoyaltyMetricRepository _metricRepository;
    private readonly IUserRepository _userRepository;

    public CreateDemoBrandsHandler(
        IAdminAccessService adminAccessService,
        IBrandMembershipService brandMembershipService,
        IBrandRepository brandRepository,
        ICoinProductRepository coinProductRepository,
        ILoyaltyMetricRepository metricRepository,
        IUserRepository userRepository)
    {
        _adminAccessService = adminAccessService;
        _brandMembershipService = brandMembershipService;
        _brandRepository = brandRepository;
        _coinProductRepository = coinProductRepository;
        _metricRepository = metricRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<bool>> Handle(
        CreateDemoBrandsCommand command,
        CancellationToken cancellationToken)
    {
        if (!_adminAccessService.IsAdmin(command.AdminTelegramUserId))
            return Result.Fail(AccessErrors.AdminRequired());

        var owner = await _userRepository.GetByIdentityAsync(
            IdentityType.Telegram,
            command.AdminTelegramUserId.ToString(),
            cancellationToken);
        if (owner is null)
            return Result.Fail(UserErrors.NotFound());

        var existingOwnedDemoBrandNames = (await _brandRepository.GetAdminBrandsAsync(cancellationToken))
            .Where(brand => brand.OwnerUserId == owner.Id)
            .Select(brand => brand.BrandName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var specs = DemoDataCatalog.BrandTemplates
            .Where(template => !existingOwnedDemoBrandNames.Contains(template.Name))
            .OrderBy(_ => Random.Shared.Next())
            .Take(5)
            .ToArray();

        foreach (var spec in specs)
        {
            var brandResult = Brand.Create(spec.Name);
            if (brandResult.IsFailed)
                return Result.Fail(brandResult.Errors);

            var brand = brandResult.Value;
            var updateResult = brand.UpdateDetails(
                spec.Name,
                spec.IsMetricsEnabled,
                spec.IsCoinsEnabled,
                spec.IsCoinProductRedemptionEnabled,
                spec.IsManualCoinRedemptionEnabled);
            if (updateResult.IsFailed)
                return Result.Fail(updateResult.Errors);

            var addBrandResult = await _brandRepository.AddAsync(brand, cancellationToken);
            if (addBrandResult.IsFailed)
                return Result.Fail(addBrandResult.Errors);

            var ownerResult = await _brandMembershipService.AssignOwnerAsync(
                brand.Id,
                owner.Id,
                cancellationToken);
            if (ownerResult.IsFailed)
                return Result.Fail(ownerResult.Errors);

            foreach (var metricSpec in spec.Metrics)
            {
                var metricResult = LoyaltyMetricDefinition.Create(
                    brand.Id,
                    metricSpec.Name,
                    metricSpec.RedemptionAmount);
                if (metricResult.IsFailed)
                    return Result.Fail(metricResult.Errors);

                _metricRepository.Add(metricResult.Value);
            }

            foreach (var productSpec in spec.Products)
            {
                var productResult = CoinProduct.Create(
                    brand.Id,
                    productSpec.Name,
                    productSpec.Price);
                if (productResult.IsFailed)
                    return Result.Fail(productResult.Errors);

                _coinProductRepository.Add(productResult.Value);
            }
        }

        await _metricRepository.SaveAsync(cancellationToken);
        await _coinProductRepository.SaveAsync(cancellationToken);

        return Result.Ok(true);
    }

}
