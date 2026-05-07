namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Тонкая обёртка над admin-операциями <c>Telegram.Bot.ITelegramBotClient</c>:
/// resolve чата, проверка прав/членства, invite-link CRUD, approve/decline join request, kick.
/// Все методы возвращают <see cref="ChatApiResult{T}"/> — никаких exception'ов на бизнес-фейлы.
///
/// Регистрируется как singleton в <c>AddTelegramBotFlow</c>.
/// </summary>
public interface IChatAdministrationApi
{
    Task<ChatApiResult<ChatInfo>> GetChatAsync(long chatId, CancellationToken ct = default);

    Task<ChatApiResult<ChatInfo>> GetChatByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Возвращает права самого бота в чате (через <c>getChatMember(chatId, botId)</c>).</summary>
    Task<ChatApiResult<BotChatPermissions>> GetBotPermissionsAsync(long chatId, CancellationToken ct = default);

    /// <summary>
    /// Резолвит статус юзера в чате. <c>NotFound</c>-семантика трактуется как
    /// <see cref="ChatMembership.NOT_MEMBER"/> — это не ошибка.
    /// </summary>
    Task<ChatApiResult<ChatMemberInfo>> GetChatMemberAsync(long chatId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Создаёт invite-link с <c>creates_join_request=true</c> — каждый клик создаёт join request,
    /// обработка через <c>chat_join_request</c> update (см. <see cref="Telegram.Bot.Types.Update.ChatJoinRequest"/>).
    /// </summary>
    Task<ChatApiResult<string>> CreateJoinRequestInviteLinkAsync(
        long chatId, string? name, CancellationToken ct = default);

    Task<ChatApiResult<bool>> RevokeChatInviteLinkAsync(long chatId, string inviteLink, CancellationToken ct = default);

    Task<ChatApiResult<bool>> ApproveChatJoinRequestAsync(long chatId, long userId, CancellationToken ct = default);

    Task<ChatApiResult<bool>> DeclineChatJoinRequestAsync(long chatId, long userId, CancellationToken ct = default);

    /// <summary>
    /// «Kick» = ban + unban сразу — юзер выкинут, но может снова вступить через invite-link.
    /// Permanent ban делается через прямой вызов <c>BanChatMember</c>.
    /// </summary>
    Task<ChatApiResult<bool>> KickChatMemberAsync(long chatId, long userId, CancellationToken ct = default);
}
