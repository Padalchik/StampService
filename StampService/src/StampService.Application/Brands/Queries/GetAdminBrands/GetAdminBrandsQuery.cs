using StampService.Application.Abstractions;
using StampService.Application.Administration;

namespace StampService.Application.Brands.Queries.GetAdminBrands;

public record GetAdminBrandsQuery(AdminActor Admin) : IQuery;
