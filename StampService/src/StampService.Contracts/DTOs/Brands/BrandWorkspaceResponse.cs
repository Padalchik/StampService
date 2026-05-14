namespace StampService.Contracts.DTOs.Brands;

public record BrandWorkspaceResponse(
    Guid BrandId,
    string BrandName,
    string RoleSystemName,
    bool IsMetricsEnabled,
    bool IsCoinsEnabled,
    bool CanIssue,
    bool CanRedeem,
    bool CanViewBalances,
    bool CanManageBrand,
    bool CanManageMetrics,
    bool CanManageStaff);
