# Phase 7: Test Coverage Gaps — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fill critical test coverage gaps: ScreenMessageRenderer, EfBotUserStore, UserTrackingMiddleware, WebhookEndpoints, concurrent sessions. Build FakeTelegramBotClient test infrastructure.

**Architecture:** FakeTelegramBotClient records all outgoing Telegram API calls for assertion. Each component gets dedicated test class with edge case coverage.

**Tech Stack:** .NET 10, xUnit, FluentAssertions, NSubstitute, Microsoft.EntityFrameworkCore.InMemory

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` section 7

**Depends on:** All previous phases

---

## File Map

**New files:**
- `tests/TelegramBotFlow.IntegrationTests/Helpers/FakeTelegramBotClient.cs` — test infrastructure
- `tests/TelegramBotFlow.Core.Tests/Screens/ScreenMessageRendererTests.cs`
- `tests/TelegramBotFlow.Core.Tests/Pipeline/UserTrackingMiddlewareTests.cs`
- `tests/TelegramBotFlow.IntegrationTests/Data/EfBotUserStoreTests.cs`
- `tests/TelegramBotFlow.IntegrationTests/WebhookEndpointTests.cs`
- `tests/TelegramBotFlow.Core.Tests/Sessions/ConcurrentSessionTests.cs`

---

### Task 1: Create FakeTelegramBotClient

**Files:**
- Create: `tests/TelegramBotFlow.IntegrationTests/Helpers/FakeTelegramBotClient.cs`

- [ ] **Step 1: Implement FakeTelegramBotClient**

Create a test double that implements `ITelegramBotClient` and records all outgoing calls. This replaces complex NSubstitute setups across tests.

```csharp
// tests/TelegramBotFlow.IntegrationTests/Helpers/FakeTelegramBotClient.cs
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Records all outgoing Telegram API calls for assertion in tests.
/// Returns reasonable defaults (Message with Id=42) for all send operations.
/// </summary>
public sealed class FakeTelegramBotClient : ITelegramBotClient
{
    private int _nextMessageId = 42;

    public ConcurrentBag<SentMessage> SentMessages { get; } = [];
    public ConcurrentBag<EditedMessage> EditedMessages { get; } = [];
    public ConcurrentBag<int> DeletedMessageIds { get; } = [];
    public ConcurrentBag<AnsweredCallback> AnsweredCallbacks { get; } = [];

    public record SentMessage(long ChatId, string? Text, ParseMode? ParseMode, IReplyMarkup? ReplyMarkup);
    public record EditedMessage(long ChatId, int MessageId, string? Text, InlineKeyboardMarkup? Keyboard);
    public record AnsweredCallback(string CallbackQueryId, string? Text, bool ShowAlert);

    // ITelegramBotClient implementation — record calls and return defaults.
    // Only implement methods actually used by the framework.
    // Throw NotImplementedException for unused methods (fail-fast in tests).

    // ... implement SendMessage, EditMessageText, DeleteMessage, AnswerCallbackQuery,
    //     SendPhoto, CopyMessage, etc.
    // Each records to the appropriate ConcurrentBag and returns a default Message.

    public void Clear()
    {
        SentMessages.Clear();
        EditedMessages.Clear();
        DeletedMessageIds.Clear();
        AnsweredCallbacks.Clear();
    }

    private Message CreateDefaultMessage(long chatId) => new()
    {
        Id = Interlocked.Increment(ref _nextMessageId),
        Date = DateTime.UtcNow,
        Chat = new Chat { Id = chatId, Type = ChatType.Private }
    };
}
```

**Note:** `ITelegramBotClient` has many methods. Only implement the ones actually called by the framework (SendMessage, EditMessageText, DeleteMessage, AnswerCallbackQuery, SendPhoto, CopyMessage, SetWebhook). For others, throw `NotImplementedException` — this surfaces any unexpected calls during tests.

- [ ] **Step 2: Build to verify compilation**

Run: `dotnet build tests/TelegramBotFlow.IntegrationTests`
Expected: Compiles

- [ ] **Step 3: Commit**

```bash
git add tests/TelegramBotFlow.IntegrationTests/Helpers/FakeTelegramBotClient.cs
git commit -m "test: add FakeTelegramBotClient test infrastructure for recording API calls"
```

---

### Task 2: ScreenMessageRenderer tests

**Files:**
- Create: `tests/TelegramBotFlow.Core.Tests/Screens/ScreenMessageRendererTests.cs`

- [ ] **Step 1: Write tests for all media transitions**

Test the critical rendering logic: what happens when screen media type changes.

```csharp
// tests/TelegramBotFlow.Core.Tests/Screens/ScreenMessageRendererTests.cs
using FluentAssertions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenMessageRendererTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly ScreenMessageRenderer _renderer;

    public ScreenMessageRendererTests()
    {
        _botClient.SendMessage(
            Arg.Any<ChatId>(), Arg.Any<string>(),
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(ci => new Message
            {
                Id = 42,
                Date = DateTime.UtcNow,
                Chat = new Chat { Id = (long)ci[0], Type = ChatType.Private }
            });

        _renderer = new ScreenMessageRenderer(_botClient);
    }

    [Fact]
    public async Task NoNavMessageId_SendsNewMessage()
    {
        var ctx = TestHelpers.CreateCallbackContext("test");
        ctx.Session = new UserSession(123);
        // NavMessageId is null — no existing message to edit

        var view = new ScreenView("Hello");

        await _renderer.RenderAsync(ctx, view);

        await _botClient.Received(1).SendMessage(
            Arg.Any<ChatId>(), "Hello",
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TextToText_EditsMessage()
    {
        var ctx = TestHelpers.CreateCallbackContext("test");
        ctx.Session = new UserSession(123);
        ctx.Session.Navigation.NavMessageId = 10;
        ctx.Session.Navigation.CurrentMediaType = ScreenMediaType.None;

        var view = new ScreenView("Updated text");

        await _renderer.RenderAsync(ctx, view);

        await _botClient.Received(1).EditMessageText(
            Arg.Any<ChatId>(), 10, "Updated text",
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<InlineKeyboardMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // Add more tests for:
    // - Text to Photo (delete old + send new)
    // - Photo to Text (delete old + send new)
    // - Photo to Photo (edit media)
    // - Edit fails (message not modified) — handle gracefully
}
```

**Note:** The exact assertions depend on the actual `ScreenMessageRenderer` implementation. Read the source to understand the transition logic, then write tests that cover each branch. The tests above are templates — fill in based on actual method signatures.

- [ ] **Step 2: Run tests, iterate until all branches covered**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~ScreenMessageRendererTests"`

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test: add ScreenMessageRenderer tests for media transition logic"
```

---

### Task 3: EfBotUserStore tests

**Files:**
- Create: `tests/TelegramBotFlow.IntegrationTests/Data/EfBotUserStoreTests.cs`

- [ ] **Step 1: Write tests with InMemory database**

```csharp
// tests/TelegramBotFlow.IntegrationTests/Data/EfBotUserStoreTests.cs
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
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/TelegramBotFlow.IntegrationTests --filter "FullyQualifiedName~EfBotUserStoreTests"`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test: add EfBotUserStore integration tests with InMemory database"
```

---

### Task 4: UserTrackingMiddleware tests

**Files:**
- Create: `tests/TelegramBotFlow.Core.Tests/Pipeline/UserTrackingMiddlewareTests.cs`

- [ ] **Step 1: Write tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Pipeline/UserTrackingMiddlewareTests.cs
using FluentAssertions;
using NSubstitute;
using Microsoft.Extensions.Caching.Memory;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class UserTrackingMiddlewareTests
{
    [Fact]
    public async Task NewUser_CreatesAndSetsOnContext()
    {
        var store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(123).Returns((TestUser?)null);
        store.CreateAsync(Arg.Any<TestUser>()).Returns(Task.CompletedTask);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        var ctx = TestHelpers.CreateMessageContext("hello", userId: 123);

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.User.Should().NotBeNull();
        ctx.User!.TelegramId.Should().Be(123);
        await store.Received(1).CreateAsync(Arg.Is<TestUser>(u => u.TelegramId == 123));
    }

    [Fact]
    public async Task ExistingUser_SetsOnContextWithoutCreate()
    {
        var existingUser = new TestUser { TelegramId = 123 };
        var store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(123).Returns(existingUser);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        var ctx = TestHelpers.CreateMessageContext("hello", userId: 123);

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        ctx.User.Should().BeSameAs(existingUser);
        await store.DidNotReceive().CreateAsync(Arg.Any<TestUser>());
    }

    [Fact]
    public async Task CachedUser_SkipsStoreCall()
    {
        var store = Substitute.For<IBotUserStore<TestUser>>();
        var existingUser = new TestUser { TelegramId = 123 };
        store.FindByTelegramIdAsync(123).Returns(existingUser);

        var middleware = new UserTrackingMiddleware<TestUser>(store);

        // First call — hits store
        var ctx1 = TestHelpers.CreateMessageContext("hello", userId: 123);
        await middleware.InvokeAsync(ctx1, _ => Task.CompletedTask);

        // Second call — should use cache
        var ctx2 = TestHelpers.CreateMessageContext("world", userId: 123);
        await middleware.InvokeAsync(ctx2, _ => Task.CompletedTask);

        await store.Received(1).FindByTelegramIdAsync(123); // Only once
        ctx2.User.Should().NotBeNull();
    }

    public class TestUser : IBotUser, new()
    {
        public long TelegramId { get; init; }
        public bool IsBlocked { get; set; }
        public DateTime JoinedAt => DateTime.UtcNow;
    }
}
```

- [ ] **Step 2: Run tests, fix as needed**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UserTrackingMiddlewareTests"`

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test: add UserTrackingMiddleware tests for user creation, caching, and context.User"
```

---

### Task 5: Concurrent session stress tests

**Files:**
- Create: `tests/TelegramBotFlow.Core.Tests/Sessions/ConcurrentSessionTests.cs`

- [ ] **Step 1: Write concurrent access test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Sessions/ConcurrentSessionTests.cs
using FluentAssertions;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class ConcurrentSessionTests
{
    [Fact]
    public async Task ParallelSessionAccess_SameUser_NoCorruption()
    {
        var store = new InMemorySessionStore();
        var lockProvider = new InMemorySessionLockProvider(
            Options.Create(new BotConfiguration { SessionLockTimeoutSeconds = 5 }));

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            using var lockHandle = await lockProvider.AcquireLockAsync(1);
            var session = await store.GetOrCreateAsync(1);
            session.Data.Set("counter", (session.Data.GetInt("counter") ?? 0) + 1);
            await store.SaveAsync(session);
        });

        await Task.WhenAll(tasks);

        var finalSession = await store.GetOrCreateAsync(1);
        finalSession.Data.GetInt("counter").Should().Be(50);
    }

    [Fact]
    public async Task ParallelSessionAccess_DifferentUsers_NoContention()
    {
        var store = new InMemorySessionStore();
        var lockProvider = new InMemorySessionLockProvider(
            Options.Create(new BotConfiguration { SessionLockTimeoutSeconds = 5 }));

        var tasks = Enumerable.Range(1, 100).Select(async userId =>
        {
            using var lockHandle = await lockProvider.AcquireLockAsync(userId);
            var session = await store.GetOrCreateAsync(userId);
            session.Data.Set("id", userId);
            await store.SaveAsync(session);
        });

        await Task.WhenAll(tasks);

        // All 100 users have their own session
        for (int i = 1; i <= 100; i++)
        {
            var s = await store.GetOrCreateAsync(i);
            s.Data.GetInt("id").Should().Be(i);
        }
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~ConcurrentSessionTests"`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test: add concurrent session access stress tests"
```

---

### Task 6: Final full verification

- [ ] **Step 1: Full build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass, 0 warnings

- [ ] **Step 2: Count total tests**

Run: `dotnet test TelegramBotFlow.slnx --logger "console;verbosity=minimal" 2>&1 | tail -5`
Expected: Significantly more tests than the original ~700
