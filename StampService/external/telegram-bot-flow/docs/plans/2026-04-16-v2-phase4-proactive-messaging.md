# Phase 4: Proactive Messaging — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add IBotNotifier for single-user proactive messaging and IBotBroadcaster for batch operations with tracking, error handling, and blocked user detection.

**Architecture:** IBotNotifier is a thin wrapper over ITelegramBotClient, registered as singleton. IBotBroadcaster uses Parallel.ForEachAsync with configurable concurrency, goes through the same rate-limited HttpClient. Both interfaces defined in Abstractions, implementations in Core.

**Tech Stack:** .NET 10, Telegram.Bot 22.9.0, xUnit, FluentAssertions, NSubstitute

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 3.1, 3.4

**Depends on:** Phase 3 completed (rate limiter on HttpClient)

---

## File Map

**New files:**
- `src/TelegramBotFlow.Core.Abstractions/Messaging/IBotNotifier.cs` — interface
- `src/TelegramBotFlow.Core.Abstractions/Messaging/IBotBroadcaster.cs` — interface + BroadcastResult, BroadcastOptions, BotMessage
- `src/TelegramBotFlow.Core/Messaging/BotNotifier.cs` — implementation
- `src/TelegramBotFlow.Core/Messaging/BotBroadcaster.cs` — implementation
- `tests/TelegramBotFlow.Core.Tests/Messaging/BotNotifierTests.cs`
- `tests/TelegramBotFlow.Core.Tests/Messaging/BotBroadcasterTests.cs`

**Modified files:**
- `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs` — register IBotNotifier, IBotBroadcaster

---

### Task 1: Define IBotNotifier interface and implement

**Files:**
- Create: `src/TelegramBotFlow.Core.Abstractions/Messaging/IBotNotifier.cs`
- Create: `src/TelegramBotFlow.Core/Messaging/BotNotifier.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Messaging/BotNotifierTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// tests/TelegramBotFlow.Core.Tests/Messaging/BotNotifierTests.cs
using FluentAssertions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Messaging;

namespace TelegramBotFlow.Core.Tests.Messaging;

public sealed class BotNotifierTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly BotNotifier _notifier;

    public BotNotifierTests()
    {
        _notifier = new BotNotifier(_botClient);
    }

    [Fact]
    public async Task SendTextAsync_CallsBotClient()
    {
        _botClient.SendMessage(
            Arg.Any<ChatId>(), Arg.Any<string>(),
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 1, Date = DateTime.UtcNow });

        await _notifier.SendTextAsync(123, "Hello");

        await _botClient.Received(1).SendMessage(
            123, "Hello",
            parseMode: ParseMode.Html,
            replyMarkup: null,
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run test — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotNotifierTests"`
Expected: FAIL — types not found

- [ ] **Step 3: Create IBotNotifier interface**

```csharp
// src/TelegramBotFlow.Core.Abstractions/Messaging/IBotNotifier.cs
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Sends messages to users outside the update pipeline (proactive messaging).
/// Registered as singleton — inject into IHostedService, background jobs, webhooks.
/// Goes through the rate-limited HttpClient automatically.
/// </summary>
public interface IBotNotifier
{
    Task<Message> SendTextAsync(long chatId, string text,
        InlineKeyboardMarkup? keyboard = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default);

    Task<Message> SendPhotoAsync(long chatId, InputFile photo,
        string? caption = null,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken ct = default);

    Task<Message> SendDocumentAsync(long chatId, InputFile document,
        string? caption = null,
        CancellationToken ct = default);

    Task<Message> CopyMessageAsync(long toChatId, long fromChatId, int messageId,
        CancellationToken ct = default);
}
```

- [ ] **Step 4: Create BotNotifier implementation**

```csharp
// src/TelegramBotFlow.Core/Messaging/BotNotifier.cs
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

internal sealed class BotNotifier : IBotNotifier
{
    private readonly ITelegramBotClient _botClient;

    public BotNotifier(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public Task<Message> SendTextAsync(long chatId, string text,
        InlineKeyboardMarkup? keyboard = null,
        ParseMode parseMode = ParseMode.Html,
        CancellationToken ct = default)
        => _botClient.SendMessage(chatId, text,
            parseMode: parseMode, replyMarkup: keyboard, cancellationToken: ct);

    public Task<Message> SendPhotoAsync(long chatId, InputFile photo,
        string? caption = null,
        InlineKeyboardMarkup? keyboard = null,
        CancellationToken ct = default)
        => _botClient.SendPhoto(chatId, photo,
            caption: caption, replyMarkup: keyboard, cancellationToken: ct);

    public Task<Message> SendDocumentAsync(long chatId, InputFile document,
        string? caption = null,
        CancellationToken ct = default)
        => _botClient.SendDocument(chatId, document,
            caption: caption, cancellationToken: ct);

    public Task<Message> CopyMessageAsync(long toChatId, long fromChatId, int messageId,
        CancellationToken ct = default)
        => _botClient.CopyMessage(toChatId, fromChatId, messageId, cancellationToken: ct);
}
```

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotNotifierTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add IBotNotifier for proactive messaging"
```

---

### Task 2: Define IBotBroadcaster and implement

**Files:**
- Create: `src/TelegramBotFlow.Core.Abstractions/Messaging/IBotBroadcaster.cs`
- Create: `src/TelegramBotFlow.Core/Messaging/BotBroadcaster.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Messaging/BotBroadcasterTests.cs`

- [ ] **Step 1: Write the tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Messaging/BotBroadcasterTests.cs
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Messaging;
using TelegramBotFlow.Core.Users;
using Telegram.Bot.Exceptions;

namespace TelegramBotFlow.Core.Tests.Messaging;

public sealed class BotBroadcasterTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly IBotUserStore<TestUser>? _userStore = Substitute.For<IBotUserStore<TestUser>>();
    private readonly BotBroadcaster _broadcaster;

    public BotBroadcasterTests()
    {
        _botClient.SendMessage(
            Arg.Any<ChatId>(), Arg.Any<string>(),
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 1, Date = DateTime.UtcNow });

        _broadcaster = new BotBroadcaster(_botClient);
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllUsers()
    {
        var chatIds = new List<long> { 100, 200, 300 };

        var result = await _broadcaster.BroadcastAsync(
            chatIds,
            _ => new BotMessage("Hello!"));

        result.TotalSent.Should().Be(3);
        result.TotalFailed.Should().Be(0);
    }

    [Fact]
    public async Task BroadcastAsync_Tracks403AsBlocked()
    {
        _botClient.SendMessage(
            200, Arg.Any<string>(),
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Forbidden: bot was blocked by the user", 403));

        var chatIds = new List<long> { 100, 200, 300 };

        var result = await _broadcaster.BroadcastAsync(
            chatIds,
            _ => new BotMessage("Hello!"));

        result.TotalSent.Should().Be(2);
        result.TotalFailed.Should().Be(1);
        result.BlockedUserIds.Should().Contain(200);
    }

    [Fact]
    public async Task BroadcastAsync_TracksOtherErrors()
    {
        _botClient.SendMessage(
            300, Arg.Any<string>(),
            parseMode: Arg.Any<ParseMode>(),
            replyMarkup: Arg.Any<IReplyMarkup>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Internal Server Error", 500));

        var chatIds = new List<long> { 100, 300 };

        var result = await _broadcaster.BroadcastAsync(
            chatIds,
            _ => new BotMessage("Hello!"));

        result.TotalSent.Should().Be(1);
        result.FailedChatIds.Should().Contain(300);
    }

    [Fact]
    public async Task BroadcastAsync_MessageFactory_ReceivesChatId()
    {
        var chatIds = new List<long> { 100 };
        long? receivedChatId = null;

        await _broadcaster.BroadcastAsync(
            chatIds,
            chatId => { receivedChatId = chatId; return new BotMessage("Hello!"); });

        receivedChatId.Should().Be(100);
    }

    public class TestUser : IBotUser
    {
        public long TelegramId { get; init; }
        public bool IsBlocked { get; set; }
        public DateTime JoinedAt => DateTime.UtcNow;
    }
}
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotBroadcasterTests"`
Expected: FAIL — types not found

- [ ] **Step 3: Create IBotBroadcaster interface and types**

```csharp
// src/TelegramBotFlow.Core.Abstractions/Messaging/IBotBroadcaster.cs
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotFlow.Core.Messaging;

/// <summary>
/// Sends messages to multiple users with concurrency control, error tracking, and blocked user detection.
/// </summary>
public interface IBotBroadcaster
{
    Task<BroadcastResult> BroadcastAsync(
        IReadOnlyList<long> chatIds,
        Func<long, BotMessage> messageFactory,
        BroadcastOptions? options = null,
        CancellationToken ct = default);
}

public record BotMessage(
    string Text,
    InlineKeyboardMarkup? Keyboard = null,
    InputFile? Photo = null,
    ParseMode ParseMode = ParseMode.Html);

public class BroadcastOptions
{
    public int MaxConcurrency { get; set; } = 25;
    public Action<long, Exception>? OnError { get; set; }
    public bool MarkBlockedUsers { get; set; } = true;
}

public record BroadcastResult(
    int TotalSent,
    int TotalFailed,
    IReadOnlyList<long> BlockedUserIds,
    IReadOnlyList<long> FailedChatIds);
```

- [ ] **Step 4: Create BotBroadcaster implementation**

```csharp
// src/TelegramBotFlow.Core/Messaging/BotBroadcaster.cs
using System.Collections.Concurrent;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotFlow.Core.Messaging;

internal sealed class BotBroadcaster : IBotBroadcaster
{
    private readonly ITelegramBotClient _botClient;

    public BotBroadcaster(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task<BroadcastResult> BroadcastAsync(
        IReadOnlyList<long> chatIds,
        Func<long, BotMessage> messageFactory,
        BroadcastOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new BroadcastOptions();

        int sent = 0;
        var blocked = new ConcurrentBag<long>();
        var failed = new ConcurrentBag<long>();

        await Parallel.ForEachAsync(chatIds,
            new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrency, CancellationToken = ct },
            async (chatId, token) =>
            {
                try
                {
                    BotMessage message = messageFactory(chatId);

                    if (message.Photo != null)
                    {
                        await _botClient.SendPhoto(chatId, message.Photo,
                            caption: message.Text, parseMode: message.ParseMode,
                            replyMarkup: message.Keyboard, cancellationToken: token);
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId, message.Text,
                            parseMode: message.ParseMode, replyMarkup: message.Keyboard,
                            cancellationToken: token);
                    }

                    Interlocked.Increment(ref sent);
                }
                catch (ApiRequestException ex) when (ex.StatusCode == 403)
                {
                    blocked.Add(chatId);
                    options.OnError?.Invoke(chatId, ex);
                }
                catch (Exception ex)
                {
                    failed.Add(chatId);
                    options.OnError?.Invoke(chatId, ex);
                }
            });

        return new BroadcastResult(
            sent,
            blocked.Count + failed.Count,
            blocked.ToArray(),
            failed.ToArray());
    }
}
```

- [ ] **Step 5: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~BotBroadcasterTests"`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add IBotBroadcaster for batch messaging with error tracking"
```

---

### Task 3: Register in DI

**Files:**
- Modify: `src/TelegramBotFlow.Core/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Register IBotNotifier and IBotBroadcaster**

In `AddTelegramBotFlow()`, add:

```csharp
services.AddSingleton<IBotNotifier, BotNotifier>();
services.AddSingleton<IBotBroadcaster, BotBroadcaster>();
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "feat: register IBotNotifier and IBotBroadcaster in DI"
```
