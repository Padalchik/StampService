using StampService.Application.Abstractions;

namespace StampService.Application.Brands.Queries.GetBrandCustomerCard;

public record GetBrandCustomerCardQuery(
    Guid RequestUserId,
    Guid BrandId,
    string CustomerPhoneNumber) : IQuery;
