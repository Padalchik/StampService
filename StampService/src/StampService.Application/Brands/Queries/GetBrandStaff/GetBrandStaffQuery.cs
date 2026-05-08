using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Queries.GetBrandStaff;

public record GetBrandStaffQuery(
    Guid ActorUserId,
    Guid BrandId) : IQuery;
