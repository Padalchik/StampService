using StampService.Application.Abstractions;
using StampService.Application.Administration;
using StampService.Application.Brands.Queries.GetMyBrands;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Common.UI;
using StampService.TelegramBot.Features.Admin.Screens;
using StampService.TelegramBot.Features.Brands.Actions;
using StampService.TelegramBot.Features.Brands.Screens;
using StampService.TelegramBot.Features.Profile.Actions;
using StampService.TelegramBot.Features.Profile.Screens;
using StampService.TelegramBot.Features.Wallet.Screens;
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

        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
        {
            return new ScreenView(
                "<b>Вход по телефону</b>\n\n" +
                "Чтобы пользоваться StampService в Telegram, подтвердите номер телефона. " +
                "Введите номер в международном формате, например <code>+79991234567</code>.")
                .AwaitInput<EnterProfilePhoneAction>();
        }

        var view = new ScreenView(
            $"{greeting}\n\n" +
            "Выберите действие:")
            .WithoutAutoMenuButton()
            .NavigateButton<MyWalletScreen>(BotMenuLabels.MyWallet);

        if (_adminAccessService.IsAdmin(ctx.UserId))
        {
            view.Row().NavigateButton<AdminPanelScreen>("Админка");
        }

        var brandsResult = await _myBrandsHandler.Handle(
            new GetMyBrandsQuery(userResult.Value.UserId),
            ctx.CancellationToken);

        if (brandsResult.IsFailed || brandsResult.Value.Brands.Count == 0)
            return view.Row().NavigateButton<ProfileScreen>(BotMenuLabels.AccountSettings);

        if (brandsResult.Value.Brands.Count == 1)
        {
            var brand = brandsResult.Value.Brands.Single();
            view.Row().Button<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>(
                BotMenuLabels.Work,
                new OpenBrandWorkspacePayload(brand.BrandId));
            return view.Row().NavigateButton<ProfileScreen>(BotMenuLabels.AccountSettings);
        }

        view.Row().NavigateButton<MyBrandsScreen>(BotMenuLabels.Workspaces);
        return view.Row().NavigateButton<ProfileScreen>(BotMenuLabels.AccountSettings);
    }
}
