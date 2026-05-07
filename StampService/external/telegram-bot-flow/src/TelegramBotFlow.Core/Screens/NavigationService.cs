using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Единственный мутатор навигационного состояния сессии.
/// Реализует <see cref="INavigationService"/> поверх <see cref="ScreenManager"/>.
/// </summary>
internal sealed class NavigationService : INavigationService
{
    private readonly ScreenManager _screenManager;

    public NavigationService(ScreenManager screenManager)
    {
        _screenManager = screenManager;
    }

    public async Task NavigateToAsync(UpdateContext context, string screenId)
    {
        await _screenManager.NavigateToAsync(context, screenId);
    }

    public async Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen
    {
        string screenId = ScreenRegistry.GetIdFromType(typeof(TScreen));
        await _screenManager.NavigateToAsync(context, screenId);
    }

    public async Task NavigateToAsync(UpdateContext context, Type screenType)
    {
        string screenId = ScreenRegistry.GetIdFromType(screenType);
        await _screenManager.NavigateToAsync(context, screenId);
    }

    public async Task NavigateBackAsync(UpdateContext context)
    {
        if (context.Session is null)
            return;

        string? previousScreen = context.Session.Navigation.NavigationStack is { Count: > 0 }
            ? context.Session.Navigation.NavigationStack[^1]
            : context.Session.Navigation.CurrentScreen;

        if (previousScreen is not null)
        {
            context.Session.Navigation.PopScreen();
            await _screenManager.RenderScreenAsync(context, previousScreen, pushToStack: false);
        }
    }

    public async Task RefreshScreenAsync(UpdateContext context)
    {
        if (context.Session?.Navigation.CurrentScreen is { } screen)
            await _screenManager.RenderScreenAsync(context, screen, pushToStack: false);
    }

    public async Task ShowViewAsync(UpdateContext context, ScreenView view)
    {
        await _screenManager.ShowViewAsync(context, view);
    }

    public async Task NavigateToRootAsync(UpdateContext context, Type screenType)
    {
        context.Session?.Navigation.Reset();
        string screenId = ScreenRegistry.GetIdFromType(screenType);
        await _screenManager.RenderScreenAsync(context, screenId, pushToStack: false);
    }

    public async Task NavigateToRootAsync(UpdateContext context, string screenId)
    {
        context.Session?.Navigation.Reset();
        await _screenManager.RenderScreenAsync(context, screenId, pushToStack: false);
    }
}