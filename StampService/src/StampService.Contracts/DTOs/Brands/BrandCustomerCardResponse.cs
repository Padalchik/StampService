using StampService.Contracts.DTOs.Wallet;

namespace StampService.Contracts.DTOs.Brands;

public record BrandCustomerCardResponse(
    Guid BrandId,
    Guid CustomerUserId,
    string CustomerName,
    string CustomerPhoneNumber,
    UserWalletBrandDetailsResponse Details);
