using FluentResults;
using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Errors;
using StampService.Contracts.DTOs.Brands;

namespace StampService.Application.Brands.Queries.GetAdminBrands;

public class GetAdminBrandsHandler : IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery>
{
    private readonly IAdminAccessService _adminAccessService;
    private readonly IBrandRepository _brandRepository;

    public GetAdminBrandsHandler(
        IAdminAccessService adminAccessService,
        IBrandRepository brandRepository)
    {
        _adminAccessService = adminAccessService;
        _brandRepository = brandRepository;
    }

    public async Task<Result<IReadOnlyCollection<AdminBrandResponse>>> Handle(
        GetAdminBrandsQuery query,
        CancellationToken cancellationToken)
    {
        if (!await _adminAccessService.IsAdminAsync(query.Admin, cancellationToken))
            return Result.Fail(AccessErrors.AdminRequired());

        var brands = await _brandRepository.GetAdminBrandsAsync(cancellationToken);
        IReadOnlyCollection<AdminBrandResponse> response = brands
            .Select(brand => new AdminBrandResponse(
                brand.BrandId,
                brand.BrandName,
                brand.IsMetricsEnabled,
                brand.IsCoinsEnabled,
                brand.IsCoinProductRedemptionEnabled,
                brand.IsManualCoinRedemptionEnabled,
                brand.OwnerUserId,
                brand.OwnerName,
                brand.OwnerPhoneNumber))
            .ToArray();

        return Result.Ok(response);
    }
}
