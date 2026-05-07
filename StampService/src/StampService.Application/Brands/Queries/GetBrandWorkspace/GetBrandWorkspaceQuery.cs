using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Queries.GetBrandWorkspace;

public record GetBrandWorkspaceQuery(
    Guid UserId,
    Guid BrandId) : IQuery;
