using System.Collections.Concurrent;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// In-memory реализация хранилища сессий на базе <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
internal sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<long, UserSession> _sessions = new();

    /// <inheritdoc />
    public Task<UserSession> GetOrCreateAsync(long userId, CancellationToken cancellationToken = default)
    {
        UserSession session = _sessions.GetOrAdd(userId, id => new UserSession(id));
        session.LastActivity = DateTime.UtcNow;

        return Task.FromResult(session);
    }

    /// <inheritdoc />
    public Task SaveAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.UserId] = session;
        return Task.CompletedTask;
    }
}