using FluentResults;
using StampService.Application.Abstractions;
using StampService.Contracts.DTOs.Brands;
using BrandEntity = StampService.Domain.Brand.Brand;

namespace StampService.Application.Brands.Commands.CreateBrand;

public class CreateBrandHandler : ICommandHandler<CreateBrandResponse, CreateBrandCommand>
{
    private readonly IBrandRepository _brandRepository;

    public CreateBrandHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    public async Task<Result<CreateBrandResponse>> Handle(
        CreateBrandCommand command,
        CancellationToken cancellationToken)
    {
        var brandResult = BrandEntity.Create(command.Request.Name);
        if (brandResult.IsFailed)
            return Result.Fail(brandResult.Errors);

        var brand = brandResult.Value;

        var addBrandResult = await _brandRepository.AddAsync(brand, cancellationToken);
        if (addBrandResult.IsFailed)
            return Result.Fail(addBrandResult.Errors);

        var response = new CreateBrandResponse(brand.Id, brand.Name, brand.CreatedAt);

        return Result.Ok(response);
    }
}
