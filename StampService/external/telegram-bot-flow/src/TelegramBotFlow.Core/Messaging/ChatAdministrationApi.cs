using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Messaging;

namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Реализация <see cref="IChatAdministrationApi"/> поверх <see cref="ITelegramBotClient"/>.
/// Нормализует Telegram-типы (<see cref="ChatMember"/>, <see cref="ChatFullInfo"/>) в наши доменные DTO,
/// маппит <see cref="ApiRequestException"/> в <see cref="ChatApiErrorCode"/> вместо bubble up.
/// </summary>
internal sealed class ChatAdministrationApi : IChatAdministrationApi
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ChatAdministrationApi> _logger;

    public ChatAdministrationApi(ITelegramBotClient bot, ILogger<ChatAdministrationApi> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task<ChatApiResult<ChatInfo>> GetChatAsync(long chatId, CancellationToken ct = default)
    {
        try
        {
            ChatFullInfo chat = await _bot.GetChat(chatId, ct);
            return ChatApiResult<ChatInfo>.Success(ToChatInfo(chat));
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "GetChat failed for {ChatId}: {Code}", chatId, ex.ErrorCode);
            return ChatApiResult<ChatInfo>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<ChatInfo>> GetChatByUsernameAsync(string username, CancellationToken ct = default)
    {
        string normalized = username.StartsWith('@') ? username : "@" + username;

        try
        {
            ChatFullInfo chat = await _bot.GetChat(normalized, ct);
            return ChatApiResult<ChatInfo>.Success(ToChatInfo(chat));
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "GetChat failed for {Username}: {Code}", normalized, ex.ErrorCode);
            return ChatApiResult<ChatInfo>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<BotChatPermissions>> GetBotPermissionsAsync(long chatId, CancellationToken ct = default)
    {
        long botId = _bot.BotId;

        try
        {
            ChatMember member = await _bot.GetChatMember(chatId, botId, ct);

            return member switch
            {
                ChatMemberAdministrator admin => ChatApiResult<BotChatPermissions>.Success(
                    new BotChatPermissions(
                        IsAdministrator: true,
                        CanInviteUsers: admin.CanInviteUsers,
                        CanRestrictMembers: admin.CanRestrictMembers,
                        CanManageChat: admin.CanManageChat)),
                ChatMemberOwner => ChatApiResult<BotChatPermissions>.Success(
                    new BotChatPermissions(true, true, true, true)),
                _ => ChatApiResult<BotChatPermissions>.Success(
                    new BotChatPermissions(false, false, false, false))
            };
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "GetChatMember (bot) failed for {ChatId}: {Code}", chatId, ex.ErrorCode);
            return ChatApiResult<BotChatPermissions>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<ChatMemberInfo>> GetChatMemberAsync(long chatId, long userId, CancellationToken ct = default)
    {
        try
        {
            ChatMember member = await _bot.GetChatMember(chatId, userId, ct);
            return ChatApiResult<ChatMemberInfo>.Success(new ChatMemberInfo(userId, NormalizeStatus(member)));
        }
        catch (ApiRequestException ex)
        {
            // 400 PARTICIPANT_ID_INVALID / "user not found" → нормализуем в NOT_MEMBER (не ошибка).
            if (ex.ErrorCode == 400)
            {
                return ChatApiResult<ChatMemberInfo>.Success(new ChatMemberInfo(userId, ChatMembership.NOT_MEMBER));
            }

            _logger.LogWarning(ex, "GetChatMember failed chat {ChatId} user {UserId}: {Code}", chatId, userId, ex.ErrorCode);
            return ChatApiResult<ChatMemberInfo>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<string>> CreateJoinRequestInviteLinkAsync(
        long chatId, string? name, CancellationToken ct = default)
    {
        try
        {
            ChatInviteLink link = await _bot.CreateChatInviteLink(
                chatId,
                name: name,
                createsJoinRequest: true,
                cancellationToken: ct);

            return ChatApiResult<string>.Success(link.InviteLink);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "CreateChatInviteLink failed for {ChatId}: {Code}", chatId, ex.ErrorCode);
            return ChatApiResult<string>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<bool>> RevokeChatInviteLinkAsync(long chatId, string inviteLink, CancellationToken ct = default)
    {
        try
        {
            await _bot.RevokeChatInviteLink(chatId, inviteLink, ct);
            return ChatApiResult<bool>.Success(true);
        }
        catch (ApiRequestException ex)
        {
            // Best-effort: link мог уже быть revoked / чат удалён — не считаем это фейлом.
            _logger.LogInformation(ex, "RevokeChatInviteLink swallowed for {ChatId}: {Code}", chatId, ex.ErrorCode);
            return ChatApiResult<bool>.Success(true);
        }
    }

    public async Task<ChatApiResult<bool>> ApproveChatJoinRequestAsync(long chatId, long userId, CancellationToken ct = default)
    {
        try
        {
            await _bot.ApproveChatJoinRequest(chatId, userId, ct);
            return ChatApiResult<bool>.Success(true);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "ApproveChatJoinRequest failed chat {ChatId} user {UserId}: {Code}", chatId, userId, ex.ErrorCode);
            return ChatApiResult<bool>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<bool>> DeclineChatJoinRequestAsync(long chatId, long userId, CancellationToken ct = default)
    {
        try
        {
            await _bot.DeclineChatJoinRequest(chatId, userId, ct);
            return ChatApiResult<bool>.Success(true);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "DeclineChatJoinRequest failed chat {ChatId} user {UserId}: {Code}", chatId, userId, ex.ErrorCode);
            return ChatApiResult<bool>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    public async Task<ChatApiResult<bool>> KickChatMemberAsync(long chatId, long userId, CancellationToken ct = default)
    {
        try
        {
            await _bot.BanChatMember(chatId, userId, untilDate: null, revokeMessages: false, cancellationToken: ct);
            await _bot.UnbanChatMember(chatId, userId, onlyIfBanned: true, cancellationToken: ct);
            return ChatApiResult<bool>.Success(true);
        }
        catch (ApiRequestException ex)
        {
            _logger.LogWarning(ex, "Kick failed chat {ChatId} user {UserId}: {Code}", chatId, userId, ex.ErrorCode);
            return ChatApiResult<bool>.Failure(ChatApiErrorCode.ChatNotReachable, ex.Message);
        }
    }

    private static ChatInfo ToChatInfo(ChatFullInfo chat) =>
        new(chat.Id, chat.Type.ToString().ToLowerInvariant(), chat.Title, chat.Username);

    private static ChatMembership NormalizeStatus(ChatMember member) =>
        member switch
        {
            ChatMemberOwner => ChatMembership.CREATOR,
            ChatMemberAdministrator => ChatMembership.ADMINISTRATOR,
            ChatMemberMember => ChatMembership.MEMBER,
            ChatMemberRestricted r when r.IsMember => ChatMembership.RESTRICTED_BUT_MEMBER,
            _ => ChatMembership.NOT_MEMBER
        };
}
