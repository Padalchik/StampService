using StampService.TelegramBot.Features.Admin;
using StampService.TelegramBot.Features.Brands.Screens;
using TelegramBotFlow.Core.Context;

namespace StampService.TelegramBot.Features.Staff;

public static class StaffBrandContext
{
    public static Guid GetBrandId(UpdateContext ctx)
    {
        var staffBrandId = ctx.Session?.Data.Get<Guid>(StaffSessionKeys.BrandId) ?? Guid.Empty;
        if (staffBrandId != Guid.Empty)
            return staffBrandId;

        var workspaceBrandId = ctx.Session?.Data.Get<Guid>(BrandWorkspaceScreen.BrandIdSessionKey) ?? Guid.Empty;
        if (workspaceBrandId != Guid.Empty)
            return workspaceBrandId;

        return ctx.Session?.Data.Get<Guid>(AdminSessionKeys.SelectedBrandId) ?? Guid.Empty;
    }

    public static string GetBrandName(UpdateContext ctx)
    {
        var staffBrandName = ctx.Session?.Data.GetString(StaffSessionKeys.BrandName);
        if (!string.IsNullOrWhiteSpace(staffBrandName))
            return staffBrandName;

        var workspaceBrandName = ctx.Session?.Data.GetString(BrandWorkspaceScreen.BrandNameSessionKey);
        if (!string.IsNullOrWhiteSpace(workspaceBrandName))
            return workspaceBrandName;

        return ctx.Session?.Data.GetString(AdminSessionKeys.SelectedBrandName) ?? "бренд";
    }
}
