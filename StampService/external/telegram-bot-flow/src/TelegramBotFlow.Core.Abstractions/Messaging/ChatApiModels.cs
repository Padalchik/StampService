namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Минимальная инфа о чате — нормализованный результат <c>getChat</c> Bot API.
/// Type — нормализованный к нижнему регистру (<c>private</c>, <c>group</c>, <c>supergroup</c>, <c>channel</c>).
/// </summary>
public sealed record ChatInfo(
    long Id,
    string Type,
    string? Title,
    string? Username);

/// <summary>
/// Нормализованный статус юзера в чате.
/// <c>RESTRICTED_BUT_MEMBER</c> — юзер restricted (например немой), но всё ещё участник.
/// </summary>
public enum ChatMembership
{
    NOT_MEMBER = 0,
    MEMBER = 1,
    ADMINISTRATOR = 2,
    CREATOR = 3,
    RESTRICTED_BUT_MEMBER = 4
}

/// <summary>
/// Снимок членства одного юзера. <see cref="IsActiveMember"/> — true для всех статусов кроме NOT_MEMBER.
/// </summary>
public sealed record ChatMemberInfo(long UserId, ChatMembership Membership)
{
    public bool IsActiveMember => Membership != ChatMembership.NOT_MEMBER;
}

/// <summary>
/// Права бота в чате — извлекаются из <see cref="Telegram.Bot.Types.ChatMember"/> при resolve permissions.
/// Если бот не админ — все флаги false.
/// </summary>
public sealed record BotChatPermissions(
    bool IsAdministrator,
    bool CanInviteUsers,
    bool CanRestrictMembers,
    bool CanManageChat);
