using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetAdminBrands;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.Errors;
using StampService.TelegramBot.Features.Admin.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Admin.Screens;

public sealed class AdminPanelScreen : IScreen
{
    private readonly IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery> _brandsHandler;

    public AdminPanelScreen(IQueryHandler<IReadOnlyCollection<AdminBrandResponse>, GetAdminBrandsQuery> brandsHandler)
    {
        _brandsHandler = brandsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var brandsResult = await _brandsHandler.Handle(
            new GetAdminBrandsQuery(ctx.UserId),
            ctx.CancellationToken);

        if (brandsResult.IsFailed)
            return new ScreenView($"Нет доступа к админке: {BotErrorFormatter.Format(brandsResult.Errors)}").BackButton();

        var view = new ScreenView(
            "<b>Админка</b>\n\n" +
            (brandsResult.Value.Count == 0
                ? "Брендов пока нет."
                : "Выберите бренд:"));

        foreach (var brand in brandsResult.Value)
        {
            var ownerText = string.IsNullOrWhiteSpace(brand.OwnerCustomerCode)
                ? "без владельца"
                : $"владелец {brand.OwnerCustomerCode}";

            view.Row().Button<OpenAdminBrandAction, OpenAdminBrandPayload>(
                $"{brand.BrandName} ({ownerText})",
                new OpenAdminBrandPayload(
                    brand.BrandId,
                    brand.BrandName,
                    brand.IsMetricsEnabled,
                    brand.IsCoinsEnabled,
                    brand.IsCoinProductRedemptionEnabled,
                    brand.IsManualCoinRedemptionEnabled,
                    brand.OwnerUserId,
                    brand.OwnerName,
                    brand.OwnerCustomerCode));
        }

        return view.Row()
            .Button<StartCreateBrandAction>("Создать бренд")
            .Row()
            .MenuButton("Главное меню")
            .BackButton();
    }
}
