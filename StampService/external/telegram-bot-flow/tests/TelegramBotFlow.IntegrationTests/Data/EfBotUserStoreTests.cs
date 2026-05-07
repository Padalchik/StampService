using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Data;

namespace TelegramBotFlow.IntegrationTests.Data;

public sealed class EfBotUserStoreTests : IDisposable
{
    private readonly BotDbContext<BotUser> _db;
    private readonly EfBotUserStore<BotUser> _store;

    public EfBotUserStoreTests()
    {
        var options = new DbContextOptionsBuilder<BotDbContext<BotUser>>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new BotDbContext<BotUser>(options);
        _store = new EfBotUserStore<BotUser>(_db);
    }

    [Fact]
    public async Task CreateAndFind_ReturnsUser()
    {
        var user = new BotUser { TelegramId = 123 };
        await _store.CreateAsync(user);
        var found = await _store.FindByTelegramIdAsync(123);
        found.Should().NotBeNull();
        found!.TelegramId.Should().Be(123);
    }

    [Fact]
    public async Task FindNonExistent_ReturnsNull()
    {
        var found = await _store.FindByTelegramIdAsync(999);
        found.Should().BeNull();
    }

    [Fact]
    public async Task MarkBlocked_SetsFlag()
    {
        var user = new BotUser { TelegramId = 123 };
        await _store.CreateAsync(user);
        await _store.MarkBlockedAsync(123);
        var found = await _store.FindByTelegramIdAsync(123);
        found!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Update_PersistsChanges()
    {
        var user = new BotUser { TelegramId = 123 };
        await _store.CreateAsync(user);
        user.IsBlocked = true;
        await _store.UpdateAsync(user);
        var found = await _store.FindByTelegramIdAsync(123);
        found!.IsBlocked.Should().BeTrue();
    }

    public void Dispose() => _db.Dispose();
}
