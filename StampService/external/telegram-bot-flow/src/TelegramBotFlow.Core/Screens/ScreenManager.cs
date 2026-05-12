using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Координирует рендер экранов, навигационный стек и состояние nav-сообщения.
/// Вызывается только из <see cref="NavigationService"/> — не является публичным API.
/// </summary>
internal sealed class ScreenManager
{
    private readonly ScreenRegistry _registry;
    private readonly IScreenMessageRenderer _messageRenderer;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ScreenManager> _logger;

    public ScreenManager(
        ScreenRegistry registry,
        IScreenMessageRenderer messageRenderer,
        ITelegramBotClient bot,
        ILogger<ScreenManager> logger)
    {
        _registry = registry;
        _messageRenderer = messageRenderer;
        _bot = bot;
        _logger = logger;
    }

    public async Task NavigateToAsync(UpdateContext ctx, string screenId)
    {
        await RenderScreenAsync(ctx, screenId, pushToStack: true);
    }

    /// <summary>
    /// Показывает временное представление без изменения текущего экрана.
    /// Автоматически добавляет кнопку «Главное меню», если в представлении нет
    /// ни одной кнопки навигации.
    /// </summary>
    public async Task ShowViewAsync(UpdateContext ctx, ScreenView view)
    {
        if (!view.HasNavigationButton)
            view.MenuButton();

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.Navigation.NavMessageId;
        ScreenMediaType oldMediaType = session?.Navigation.CurrentMediaType ?? ScreenMediaType.None;

        _logger.LogDebug(
            "Showing action view for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            ctx.UserId, oldMediaType, view.MediaType, existingMessageId);

        var (keyboard, payloads) = view.BuildKeyboardWithPayloads();

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, keyboard, existingMessageId, oldMediaType, view.MediaType);

        if (session is not null)
        {
            session.Navigation.NavMessageId = sentMessage.Id;
            session.Navigation.CurrentMediaType = view.MediaType;
            session.Navigation.IsActionViewActive = true;

            session.Navigation.SetPending(view.PendingInputActionId);

            foreach (KeyValuePair<string, string> kvp in payloads)
                session.Navigation.StorePayloadJson(kvp.Key, kvp.Value);

            await HandleReplyKeyboardAsync(ctx, view, session);
        }
    }

    /// <summary>
    /// Рендерит экран по ID и синхронизирует навигационное состояние сессии.
    /// Автоматически добавляет кнопку «← Назад», если стек навигации не пуст и в
    /// представлении нет ни одной кнопки навигации.
    /// </summary>
    internal async Task RenderScreenAsync(UpdateContext ctx, string screenId, bool pushToStack)
    {
        IScreen screen = _registry.Resolve(screenId, ctx.RequestServices);
        ScreenView view = await screen.RenderAsync(ctx);

        UserSession? session = ctx.Session;
        int? existingMessageId = session?.Navigation.NavMessageId;
        ScreenMediaType oldMediaType = session?.Navigation.CurrentMediaType ?? ScreenMediaType.None;
        ScreenMediaType newMediaType = view.MediaType;

        _logger.LogDebug(
            "Rendering screen '{ScreenId}' for user {UserId}. Old media: {OldMedia}, New media: {NewMedia}, NavMsg: {NavMsgId}",
            screenId, ctx.UserId, oldMediaType, newMediaType, existingMessageId);

        var (keyboard, payloads) = view.BuildKeyboardWithPayloads();

        Message sentMessage =
            await _messageRenderer.RenderAsync(ctx, view, keyboard, existingMessageId, oldMediaType, newMediaType);

        if (session is not null)
        {
            if (pushToStack)
                session.Navigation.PushScreen(screenId); // PushScreen clears PendingInputActionId
            else
                session.Navigation.CurrentScreen = screenId;

            session.Navigation.NavMessageId = sentMessage.Id;
            session.Navigation.CurrentMediaType = newMediaType;
            session.Navigation.IsActionViewActive = false;

            // View может явно задать pending input; если не задал, очищаем старое ожидание.
            session.Navigation.SetPending(view.PendingInputActionId);

            foreach (KeyValuePair<string, string> kvp in payloads)
                session.Navigation.StorePayloadJson(kvp.Key, kvp.Value);

            await HandleReplyKeyboardAsync(ctx, view, session);

            session.Navigation.ClearNavigationArgs();
        }
    }

    /// <summary>
    /// Sends or removes reply keyboard based on ScreenView configuration.
    /// Reply keyboard requires a separate message because Telegram doesn't allow
    /// InlineKeyboardMarkup and ReplyKeyboardMarkup on the same message.
    /// </summary>
    private async Task HandleReplyKeyboardAsync(UpdateContext ctx, ScreenView view, UserSession session)
    {
        bool hadReplyKeyboard = session.Navigation.HasActiveReplyKeyboard;
        bool wantsReplyKeyboard = view.ReplyKeyboard != null;
        bool wantsRemove = view.ShouldRemoveReplyKeyboard;

        if (wantsReplyKeyboard)
        {
            ReplyKeyboardMarkup markup = view.ReplyKeyboard!.Resize().Build();
            try
            {
                await _bot.SendMessage(
                    ctx.ChatId,
                    "\u200B", // zero-width space — invisible carrier message
                    replyMarkup: markup,
                    cancellationToken: ctx.CancellationToken);
                session.Navigation.HasActiveReplyKeyboard = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send reply keyboard for user {UserId}", ctx.UserId);
            }
        }
        else if (hadReplyKeyboard && (wantsRemove || !wantsReplyKeyboard))
        {
            try
            {
                await _bot.SendMessage(
                    ctx.ChatId,
                    "\u200B",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: ctx.CancellationToken);
                session.Navigation.HasActiveReplyKeyboard = false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove reply keyboard for user {UserId}", ctx.UserId);
            }
        }
    }
}
