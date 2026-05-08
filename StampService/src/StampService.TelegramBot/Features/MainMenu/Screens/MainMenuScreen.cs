using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Brands.Queries.GetMyBrands;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Features.Admin.Screens;
using StampService.TelegramBot.Features.Brands.Actions;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.CustomerCode.Screens;
using StampService.TelegramBot.Features.MetricBalances.Screens;
using StampService.TelegramBot.Features.RedemptionCode.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.MainMenu.Screens;

public sealed class MainMenuScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<MyBrandsResponse, GetMyBrandsQuery> _myBrandsHandler;
    private readonly IAdminAccessService _adminAccessService;

    public MainMenuScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MyBrandsResponse, GetMyBrandsQuery> myBrandsHandler,
        IAdminAccessService adminAccessService)
    {
        _ensureUserHandler = ensureUserHandler;
        _myBrandsHandler = myBrandsHandler;
        _adminAccessService = adminAccessService;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var username = from?.Username;
        var greeting = string.IsNullOrWhiteSpace(username)
            ? "Вы авторизованы в StampService."
            : $"@{username}, вы авторизованы в StampService.";

        var view = new ScreenView(
            $"{greeting}\n\n" +
            "Выберите действие:")
            .NavigateButton<MyCustomerCodeScreen>("Мой код")
            .Row()
            .NavigateButton<MyRedemptionCodeScreen>("Код для списания")
            .Row()
            .NavigateButton<MyBalancesScreen>("Мои балансы");

        if (_adminAccessService.IsAdmin(ctx.UserId))
        {
            view.Row().NavigateButton<AdminPanelScreen>("Админка");
        }

        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return view;

        var brandsResult = await _myBrandsHandler.Handle(
            new GetMyBrandsQuery(userResult.Value.UserId),
            ctx.CancellationToken);

        if (brandsResult.IsFailed || brandsResult.Value.Brands.Count == 0)
            return view;

        if (brandsResult.Value.Brands.Count == 1)
        {
            var brand = brandsResult.Value.Brands.Single();
            return view.Row().Button<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>(
                $"Бренд: {brand.BrandName}",
                new OpenBrandWorkspacePayload(brand.BrandId));
        }

        return view.Row().NavigateButton<MyBrandsScreen>("Рабочие бренды");
    }
}
