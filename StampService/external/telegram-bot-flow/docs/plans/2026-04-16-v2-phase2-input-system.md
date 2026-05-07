# Phase 2: Input System Expansion — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Expand input handling beyond text-only: add typed media/contact/location properties to UpdateContext, make PendingInputMiddleware accept any message type, add Reply Keyboard support to ScreenView.

**Architecture:** UpdateContext extracts all message content types at construction. PendingInputMiddleware routes any Message update (not just text) when input is pending. ScreenView gains WithReplyKeyboard() method, ScreenMessageRenderer handles reply keyboard lifecycle.

**Tech Stack:** .NET 10, Telegram.Bot 22.9.0, xUnit, FluentAssertions, NSubstitute

**Spec:** `docs/specs/2026-04-16-v2-foundation-design.md` sections 2.1–2.4

**Depends on:** Phase 1 completed

---

## File Map

**Modified files:**
- `src/TelegramBotFlow.Core.Abstractions/Context/UpdateContext.cs` — add Photos, Document, Contact, Location, Voice, VideoNote, Video, HasMedia
- `src/TelegramBotFlow.Core/Pipeline/Middlewares/PendingInputMiddleware.cs` — accept any Message, not just text
- `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenView.cs` — add WithReplyKeyboard(), RemoveReplyKeyboard()
- `src/TelegramBotFlow.Core/Screens/ScreenMessageRenderer.cs` — handle reply keyboard send/remove
- `tests/TelegramBotFlow.Core.Tests/Context/UpdateContextFactsTests.cs` — test media extraction
- `tests/TelegramBotFlow.Core.Tests/Pipeline/PendingInputMiddlewareTests.cs` — test non-text input routing
- `tests/TelegramBotFlow.Core.Tests/` — new ScreenView reply keyboard tests

---

### Task 1: Add typed input properties to UpdateContext

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Context/UpdateContext.cs`
- Modify: `tests/TelegramBotFlow.Core.Tests/Context/UpdateContextFactsTests.cs`

- [ ] **Step 1: Write tests for media extraction**

```csharp
// Add to UpdateContextFactsTests.cs:
[Fact]
public void PhotoMessage_ExtractsPhotos()
{
    var photos = new[] { new PhotoSize { FileId = "photo1", Width = 100, Height = 100, FileUniqueId = "u1" } };
    var update = new Update
    {
        Message = new Message
        {
            Photo = photos,
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = ChatType.Private },
            Date = DateTime.UtcNow,
            Id = 1
        }
    };

    var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());

    ctx.Photos.Should().BeSameAs(photos);
    ctx.MessageText.Should().BeNull();
    ctx.HasMedia.Should().BeTrue();
}

[Fact]
public void DocumentMessage_ExtractsDocument()
{
    var doc = new Document { FileId = "doc1", FileName = "test.pdf", FileUniqueId = "u2" };
    var update = new Update
    {
        Message = new Message
        {
            Document = doc,
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = ChatType.Private },
            Date = DateTime.UtcNow,
            Id = 1
        }
    };

    var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());

    ctx.Document.Should().BeSameAs(doc);
    ctx.HasMedia.Should().BeTrue();
}

[Fact]
public void ContactMessage_ExtractsContact()
{
    var contact = new Contact { PhoneNumber = "+1234567890", FirstName = "John" };
    var update = new Update
    {
        Message = new Message
        {
            Contact = contact,
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = ChatType.Private },
            Date = DateTime.UtcNow,
            Id = 1
        }
    };

    var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());

    ctx.Contact.Should().BeSameAs(contact);
    ctx.HasMedia.Should().BeFalse(); // Contact is not media
}

[Fact]
public void LocationMessage_ExtractsLocation()
{
    var location = new Location { Latitude = 55.75f, Longitude = 37.62f };
    var update = new Update
    {
        Message = new Message
        {
            Location = location,
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = ChatType.Private },
            Date = DateTime.UtcNow,
            Id = 1
        }
    };

    var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());

    ctx.Location.Should().BeSameAs(location);
}

[Fact]
public void TextMessage_HasNoMedia()
{
    var ctx = TestHelpers.CreateMessageContext("hello");

    ctx.Photos.Should().BeNull();
    ctx.Document.Should().BeNull();
    ctx.Contact.Should().BeNull();
    ctx.Location.Should().BeNull();
    ctx.HasMedia.Should().BeFalse();
}
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UpdateContextFactsTests" --no-restore`
Expected: FAIL — properties not found

- [ ] **Step 3: Add properties to UpdateContext**

In `UpdateContext.cs`, add properties and extraction in constructor:

```csharp
// Properties (after MessageText):
public PhotoSize[]? Photos { get; }
public Document? Document { get; }
public Contact? Contact { get; }
public Location? Location { get; }
public Voice? Voice { get; }
public VideoNote? VideoNote { get; }
public Video? Video { get; }

public bool HasMedia => Photos != null || Document != null
                     || Voice != null || Video != null || VideoNote != null;

// In constructor, after MessageText extraction:
Photos = update.Message?.Photo;
Document = update.Message?.Document;
Contact = update.Message?.Contact;
Location = update.Message?.Location;
Voice = update.Message?.Voice;
VideoNote = update.Message?.VideoNote;
Video = update.Message?.Video;
```

- [ ] **Step 4: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~UpdateContextFactsTests"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add typed input properties to UpdateContext (Photos, Document, Contact, Location, Voice, Video)"
```

---

### Task 2: PendingInputMiddleware accepts any Message

**Files:**
- Modify: `src/TelegramBotFlow.Core/Pipeline/Middlewares/PendingInputMiddleware.cs`
- Modify: `tests/TelegramBotFlow.Core.Tests/Pipeline/PendingInputMiddlewareTests.cs`

- [ ] **Step 1: Write test for photo input routing**

```csharp
// Add to PendingInputMiddlewareTests.cs:
[Fact]
public async Task PhotoMessage_WithPendingAction_RoutesToHandler()
{
    var handlerCalled = false;
    var registry = new InputHandlerRegistry();
    registry.Register("photo_action", _ => { handlerCalled = true; return Task.CompletedTask; });

    var middleware = new PendingInputMiddleware(registry, Substitute.For<ILogger<PendingInputMiddleware>>());

    // Create photo message context (no MessageText)
    var update = new Update
    {
        Message = new Message
        {
            Photo = [new PhotoSize { FileId = "p1", Width = 100, Height = 100, FileUniqueId = "u1" }],
            From = new User { Id = 123, FirstName = "Test" },
            Chat = new Chat { Id = 456, Type = ChatType.Private },
            Date = DateTime.UtcNow,
            Id = 1
        }
    };
    var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());
    ctx.Session = new UserSession(123);
    ctx.Session.Navigation.PendingInputActionId = "photo_action";

    await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

    handlerCalled.Should().BeTrue();
}
```

- [ ] **Step 2: Run test — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "PhotoMessage_WithPendingAction"`
Expected: FAIL — photo message ignored because `MessageText == null`

- [ ] **Step 3: Fix PendingInputMiddleware**

In `PendingInputMiddleware.cs`, change the text-only check. The current logic at line 19 checks `UpdateType != Message` which is correct. The issue is at line 26 — command check uses `MessageText?.StartsWith('/')` which is fine (photos don't have MessageText). But the actionId check at line 34 reads `PendingInputActionId` only after the text-null guard... wait, re-reading the code: there IS no text-null guard. The middleware already routes any message with a pending action. Let me re-check.

Actually, looking at the code again: lines 17-38 show that the middleware:
1. Skips non-Message updates (line 19) — correct
2. Skips commands (line 26) — only if `MessageText?.StartsWith('/')` — photos won't match, correct
3. Gets `actionId` (line 34) — checks session
4. Routes if handler found (line 40-50)

So the middleware already handles non-text messages! The test should pass. Let me verify by examining more carefully... Yes, the middleware routes ANY message update when `PendingInputActionId` is set. No change needed.

Update the test to verify current behavior works, then commit as a coverage addition.

- [ ] **Step 4: Run test — expect pass (middleware already handles this)**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "PhotoMessage_WithPendingAction"`
Expected: PASS — middleware already routes non-text messages

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: verify PendingInputMiddleware routes photo/media messages with pending action"
```

---

### Task 3: Reply Keyboard support in ScreenView

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenView.cs`
- Modify: `src/TelegramBotFlow.Core.Abstractions/UI/ReplyKeyboard.cs`
- Modify: `src/TelegramBotFlow.Core/Screens/ScreenMessageRenderer.cs`
- Create: `tests/TelegramBotFlow.Core.Tests/Screens/ScreenViewReplyKeyboardTests.cs`

- [ ] **Step 1: Write tests**

```csharp
// tests/TelegramBotFlow.Core.Tests/Screens/ScreenViewReplyKeyboardTests.cs
using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenViewReplyKeyboardTests
{
    [Fact]
    public void WithReplyKeyboard_SetsReplyMarkup()
    {
        var view = new ScreenView("Test")
            .WithReplyKeyboard(kb => kb.RequestContact("Share phone"));

        view.ReplyKeyboard.Should().NotBeNull();
    }

    [Fact]
    public void RemoveReplyKeyboard_SetsRemoveFlag()
    {
        var view = new ScreenView("Test")
            .RemoveReplyKeyboard();

        view.ShouldRemoveReplyKeyboard.Should().BeTrue();
    }

    [Fact]
    public void Default_NoReplyKeyboard()
    {
        var view = new ScreenView("Test");

        view.ReplyKeyboard.Should().BeNull();
        view.ShouldRemoveReplyKeyboard.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests — expect failure**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~ScreenViewReplyKeyboardTests"`
Expected: FAIL — properties/methods not found

- [ ] **Step 3: Add Reply Keyboard support to ScreenView**

In `ScreenView.cs`, add:

```csharp
// Properties:
internal Func<ReplyKeyboard, ReplyKeyboard>? ReplyKeyboardFactory { get; private set; }
public bool ShouldRemoveReplyKeyboard { get; private set; }

// Computed property for external access:
public ReplyKeyboard? ReplyKeyboard =>
    ReplyKeyboardFactory != null ? ReplyKeyboardFactory(new ReplyKeyboard()) : null;

// Builder methods:
public ScreenView WithReplyKeyboard(Func<ReplyKeyboard, ReplyKeyboard> configure)
{
    ReplyKeyboardFactory = configure;
    ShouldRemoveReplyKeyboard = false;
    return this;
}

public ScreenView RemoveReplyKeyboard()
{
    ReplyKeyboardFactory = null;
    ShouldRemoveReplyKeyboard = true;
    return this;
}
```

- [ ] **Step 4: Run tests — expect pass**

Run: `dotnet test tests/TelegramBotFlow.Core.Tests --filter "FullyQualifiedName~ScreenViewReplyKeyboardTests"`
Expected: All pass

- [ ] **Step 5: Update ScreenMessageRenderer to handle reply keyboard**

In `ScreenMessageRenderer.cs`, after sending/editing the navigation message (inline keyboard), add logic to handle reply keyboard:

```csharp
// After main message send/edit:
if (view.ReplyKeyboard != null)
{
    var replyMarkup = view.ReplyKeyboard.Resize().Build();
    await botClient.SendMessage(context.ChatId, "\u200B", replyMarkup: replyMarkup, cancellationToken: ct);
}
else if (view.ShouldRemoveReplyKeyboard)
{
    await botClient.SendMessage(context.ChatId, "\u200B", replyMarkup: ReplyKeyboard.Remove(), cancellationToken: ct);
}
```

**Note:** This sends a zero-width space message with the reply keyboard. This is a known Telegram pattern — reply keyboards can only be sent with a new message, not edited. The implementation may need refinement based on UX testing with real Telegram clients.

- [ ] **Step 6: Build and test**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add Reply Keyboard support to ScreenView and ScreenMessageRenderer"
```

---

### Task 4: Final verification

- [ ] **Step 1: Full test suite**

Run: `dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx`
Expected: All pass, 0 warnings
