using System.Net;
using StampService.Application.Abstractions;
using StampService.Application.Brands.Queries.GetMyBrands;
using StampService.Application.Users.Commands.EnsureTelegramUser;
using StampService.Contracts.DTOs.Brands;
using StampService.TelegramBot.Features.Brands.Actions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace StampService.TelegramBot.Features.Brands.Screens;

public sealed class MyBrandsScreen : IScreen
{
    private readonly ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> _ensureUserHandler;
    private readonly IQueryHandler<MyBrandsResponse, GetMyBrandsQuery> _myBrandsHandler;

    public MyBrandsScreen(
        ICommandHandler<EnsureTelegramUserResponse, EnsureTelegramUserCommand> ensureUserHandler,
        IQueryHandler<MyBrandsResponse, GetMyBrandsQuery> myBrandsHandler)
    {
        _ensureUserHandler = ensureUserHandler;
        _myBrandsHandler = myBrandsHandler;
    }

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var from = ctx.Update.Message?.From ?? ctx.Update.CallbackQuery?.From;
        var userResult = await _ensureUserHandler.Handle(
            new EnsureTelegramUserCommand(
                ctx.UserId,
                from?.FirstName,
                from?.LastName,
                from?.Username),
            ctx.CancellationToken);

        if (userResult.IsFailed)
            return new ScreenView("Не удалось определить пользователя.").BackButton();

        var brandsResult = await _myBrandsHandler.Handle(
            new GetMyBrandsQuery(userResult.Value.UserId),
            ctx.CancellationToken);

        if (brandsResult.IsFailed)
            return new ScreenView("Не удалось загрузить бренды.").BackButton();

        if (brandsResult.Value.Brands.Count == 0)
        {
            return new ScreenView(
                "<b>Рабочие бренды</b>\n\n" +
                "Вы пока не добавлены ни в один бренд.")
                .BackButton();
        }

        var view = new ScreenView("<b>Рабочие бренды</b>\n\nВыберите бренд:");
        foreach (var brand in brandsResult.Value.Brands)
        {
            view.Row().Button<OpenBrandWorkspaceAction, OpenBrandWorkspacePayload>(
                $"{brand.BrandName} ({brand.RoleSystemName})",
                new OpenBrandWorkspacePayload(brand.BrandId));
        }

        return view.BackButton();
    }

    private static string Html(string value)
    {
        return WebUtility.HtmlEncode(value);
    }
}
