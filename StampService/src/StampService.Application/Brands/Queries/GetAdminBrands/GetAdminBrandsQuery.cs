using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Queries.GetAdminBrands;

public record GetAdminBrandsQuery(long AdminTelegramUserId) : IQuery;
