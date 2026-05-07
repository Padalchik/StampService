namespace StampService.Contracts.DTOs.Brands;

public record BrandWorkspaceResponse(
    Guid BrandId,
    string BrandName,
    string RoleSystemName,
    bool CanIssue,
    bool CanRedeem,
    bool CanViewBalances,
    bool CanManageMetrics,
    bool CanManageStaff);
