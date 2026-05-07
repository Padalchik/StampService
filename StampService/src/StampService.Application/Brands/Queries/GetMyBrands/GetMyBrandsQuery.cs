using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Queries.GetMyBrands;

public record GetMyBrandsQuery(Guid UserId) : IQuery;
