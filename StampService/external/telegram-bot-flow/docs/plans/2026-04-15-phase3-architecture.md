# Phase 3: Architecture + API — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean, extensible API. IBotUserStore abstraction (Identity-style). Production features (webhook secret, payload TTL). Middleware ordering validation. Attribute-based ID overrides.

**Architecture:** Extract user storage abstraction to Core.Abstractions, move EF Core implementation to renamed Data.Postgres package, add production features to BotConfiguration/BotApplication, add attributes for explicit screen/action IDs.

**Tech Stack:** .NET 10, Telegram.Bot 22.9.0, EF Core 10, xUnit

---

### Task 1: IBotUser interface and IBotUserStore\<TUser\> abstraction

Add user storage abstraction to Core.Abstractions. Move UserTrackingMiddleware from Core.Data to Core (using the abstraction, not EF Core).

**Files:**
- Create: `src/TelegramBotFlow.Core.Abstractions/Users/IBotUser.cs`
- Create: `src/TelegramBotFlow.Core.Abstractions/Users/IBotUserStore.cs`
- Modify: `src/TelegramBotFlow.Core.Data/BotUser.cs` — implement IBotUser
- Move + Refactor: `src/TelegramBotFlow.Core.Data/Middleware/UserTrackingMiddleware.cs` → `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs` (use IBotUserStore instead of BotDbContext)

- [ ] **Step 1: Create IBotUser interface**

Create `src/TelegramBotFlow.Core.Abstractions/Users/IBotUser.cs`:

```csharp
namespace TelegramBotFlow.Core.Users;

/// <summary>
/// Base contract for a bot user entity.
/// Implement in your user class to use with IBotUserStore.
/// </summary>
public interface IBotUser
{
    long TelegramId { get; }
    bool IsBlocked { get; set; }
    DateTime JoinedAt { get; }
}
```

- [ ] **Step 2: Create IBotUserStore\<TUser\> interface**

Create `src/TelegramBotFlow.Core.Abstractions/Users/IBotUserStore.cs`:

```csharp
namespace TelegramBotFlow.Core.Users;

/// <summary>
/// Storage abstraction for bot users, analogous to ASP.NET Identity's IUserStore.
/// </summary>
public interface IBotUserStore<TUser> where TUser : class, IBotUser
{
    Task<TUser?> FindByTelegramIdAsync(long telegramId, CancellationToken ct = default);
    Task CreateAsync(TUser user, CancellationToken ct = default);
    Task UpdateAsync(TUser user, CancellationToken ct = default);
    Task MarkBlockedAsync(long telegramId, CancellationToken ct = default);
}
```

- [ ] **Step 3: Make BotUser implement IBotUser**

In `src/TelegramBotFlow.Core.Data/BotUser.cs`, add interface:

```csharp
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Data;

public class BotUser : IBotUser
{
    public long TelegramId { get; init; }
    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;
    public bool IsBlocked { get; set; }
}
```

- [ ] **Step 4: Create new UserTrackingMiddleware in Core using IBotUserStore**

Create `src/TelegramBotFlow.Core/Pipeline/Middlewares/UserTrackingMiddleware.cs`:

```csharp
using Microsoft.Extensions.Caching.Memory;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware that tracks new users via IBotUserStore.
/// Creates user on first interaction, caches known users for 1 hour.
/// </summary>
public sealed class UserTrackingMiddleware<TUser> : IUpdateMiddleware
    where TUser : class, IBotUser, new()
{
    private static readonly MemoryCache _knownUsers = new(new MemoryCacheOptions());
    private static readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1)
    };

    private readonly IBotUserStore<TUser> _userStore;

    public UserTrackingMiddleware(IBotUserStore<TUser> userStore)
    {
        _userStore = userStore;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId != 0)
        {
            long userId = context.UserId;
            if (!_knownUsers.TryGetValue(userId, out _))
            {
                TUser? existing = await _userStore.FindByTelegramIdAsync(userId, context.CancellationToken);
                if (existing is null)
                {
                    await _userStore.CreateAsync(
                        new TUser { TelegramId = userId },
                        context.CancellationToken);
                }

                _knownUsers.Set(userId, true, _cacheOptions);
            }
        }

        await next(context);
    }
}
```

- [ ] **Step 5: Keep old UserTrackingMiddleware in Core.Data as EF-based wrapper (backward compat)**

Keep the existing middleware in Core.Data but mark it as obsolete, or better — delete it and have Core.Data's DI extension register the new Core middleware with EfBotUserStore. This will be done in Task 2 when we create the EF store.

For now: delete `src/TelegramBotFlow.Core.Data/Middleware/UserTrackingMiddleware.cs`. Update `src/TelegramBotFlow.Core.Data/DependencyInjectionExtensions.cs` to NOT register the old middleware (we'll fix registration in Task 2).

- [ ] **Step 6: Add Microsoft.Extensions.Caching.Memory to Core.csproj**

Add PackageReference to `src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj`:
```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" />
```

- [ ] **Step 7: Build (may have errors until Task 2 completes DI registration)**

```bash
dotnet build TelegramBotFlow.slnx
```

Fix any compilation issues. The App project will need updating in Task 2.

- [ ] **Step 8: Commit**

```bash
git add -A && git commit -m "feat: add IBotUser/IBotUserStore abstractions, move UserTrackingMiddleware to Core"
```

---

### Task 2: Rename Core.Data to Data.Postgres, implement EfBotUserStore

Rename the package, implement IBotUserStore via EF Core, update DI registration.

**Files:**
- Rename: `src/TelegramBotFlow.Core.Data/` → `src/TelegramBotFlow.Data.Postgres/`
- Create: `src/TelegramBotFlow.Data.Postgres/EfBotUserStore.cs`
- Modify: `src/TelegramBotFlow.Data.Postgres/DependencyInjectionExtensions.cs`
- Modify: `TelegramBotFlow.slnx` — update project path
- Modify: `src/TelegramBotFlow.App/TelegramBotFlow.App.csproj` — update reference
- Modify: test projects — update references
- Modify: `src/TelegramBotFlow.App/Program.cs` — update using/registration

- [ ] **Step 1: Rename directory and update .csproj**

```bash
mv src/TelegramBotFlow.Core.Data src/TelegramBotFlow.Data.Postgres
```

Rename the .csproj file:
```bash
mv src/TelegramBotFlow.Data.Postgres/TelegramBotFlow.Core.Data.csproj src/TelegramBotFlow.Data.Postgres/TelegramBotFlow.Data.Postgres.csproj
```

Update the .csproj:
- Change `<RootNamespace>` to `TelegramBotFlow.Data.Postgres` (or keep old namespace for compat)
- Update project reference from Core to Core.Abstractions (since it only needs IBotUserStore now... actually it still needs Core for IUpdateMiddleware)

Update `TelegramBotFlow.slnx`:
```xml
<Project Path="src/TelegramBotFlow.Data.Postgres/TelegramBotFlow.Data.Postgres.csproj" />
```

Update all ProjectReference paths in App.csproj and test .csproj files.

- [ ] **Step 2: Create EfBotUserStore**

Create `src/TelegramBotFlow.Data.Postgres/EfBotUserStore.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Data.Postgres;

/// <summary>
/// EF Core implementation of IBotUserStore.
/// </summary>
public sealed class EfBotUserStore<TUser> : IBotUserStore<TUser>
    where TUser : class, IBotUser, new()
{
    private readonly BotDbContext<TUser> _db;

    public EfBotUserStore(BotDbContext<TUser> db) => _db = db;

    public async Task<TUser?> FindByTelegramIdAsync(long telegramId, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);

    public async Task CreateAsync(TUser user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TUser user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkBlockedAsync(long telegramId, CancellationToken ct = default)
    {
        TUser? user = await FindByTelegramIdAsync(telegramId, ct);
        if (user is not null)
        {
            user.IsBlocked = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
```

- [ ] **Step 3: Update DependencyInjectionExtensions**

Register EfBotUserStore and the new Core UserTrackingMiddleware:

```csharp
public static IServiceCollection AddBotCoreData(...)
{
    // ... existing DbContext registration ...
    services.AddScoped<IBotUserStore<BotUser>, EfBotUserStore<BotUser>>();
    services.AddTransient<UserTrackingMiddleware<BotUser>>();
    return services;
}
```

- [ ] **Step 4: Update App Program.cs**

Update usings from `TelegramBotFlow.Core.Data` to `TelegramBotFlow.Data.Postgres`. Update `Use<UserTrackingMiddleware>()` to use the Core version.

- [ ] **Step 5: Update namespaces across all files in the renamed project**

Either keep old `TelegramBotFlow.Core.Data` namespace (less churn) or update to `TelegramBotFlow.Data.Postgres`. Recommendation: keep old namespace for now, change just the project/package name. This minimizes changes.

- [ ] **Step 6: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: rename Core.Data to Data.Postgres, add EfBotUserStore

IBotUserStore<TUser> implemented via EF Core. Package renamed to
TelegramBotFlow.Data.Postgres to match NuGet naming convention."
```

---

### Task 3: Webhook secret token + BotConfiguration additions

Add webhook secret token validation and payload cache configuration.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs`
- Modify: `src/TelegramBotFlow.Core/Hosting/BotRuntime.cs`
- Modify: `src/TelegramBotFlow.Core/Hosting/WebhookEndpoints.cs`

- [ ] **Step 1: Add new properties to BotConfiguration**

```csharp
public sealed class BotConfiguration
{
    // ... existing ...
    public string? WebhookSecretToken { get; set; }
    public int PayloadMaxCount { get; set; } = 500;
    public TimeSpan? PayloadTtl { get; set; }
}
```

- [ ] **Step 2: Pass secret token in SetWebhook call**

In `BotRuntime.ConfigureWebhookAsync()`, pass secret token:

```csharp
await bot.SetWebhook(config.WebhookUrl, secretToken: config.WebhookSecretToken, ...);
```

- [ ] **Step 3: Validate secret token in webhook endpoint**

In `WebhookEndpoints.cs`, add header validation:

```csharp
public static async Task HandleWebhookUpdate(HttpContext httpContext, ...)
{
    var config = httpContext.RequestServices.GetRequiredService<IOptions<BotConfiguration>>().Value;
    if (config.WebhookSecretToken is not null)
    {
        string? headerToken = httpContext.Request.Headers["X-Telegram-Bot-Api-Secret-Token"];
        if (headerToken != config.WebhookSecretToken)
        {
            httpContext.Response.StatusCode = 403;
            return;
        }
    }
    // ... existing processing ...
}
```

- [ ] **Step 4: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: webhook secret token validation and payload config options"
```

---

### Task 4: ActionId and ScreenId attributes

Add attributes for explicit ID override on actions and screens.

**Files:**
- Create: `src/TelegramBotFlow.Core.Abstractions/Endpoints/ActionIdAttribute.cs`
- Create: `src/TelegramBotFlow.Core.Abstractions/Screens/ScreenIdAttribute.cs`
- Modify: `src/TelegramBotFlow.Core/Screens/ScreenRegistry.cs` — respect ScreenIdAttribute
- Modify: `src/TelegramBotFlow.Core/Screens/ScreenView.cs` or wherever action type names are resolved — respect ActionIdAttribute
- Create: `tests/TelegramBotFlow.Core.Tests/Screens/ScreenIdAttributeTests.cs`

- [ ] **Step 1: Create ActionIdAttribute**

```csharp
namespace TelegramBotFlow.Core.Endpoints;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ActionIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
```

- [ ] **Step 2: Create ScreenIdAttribute**

```csharp
namespace TelegramBotFlow.Core.Screens;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ScreenIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}
```

- [ ] **Step 3: Update ScreenRegistry to respect ScreenIdAttribute**

In `ScreenRegistry.Register(Type screenType)`, check for attribute:

```csharp
public void Register(Type screenType)
{
    var attr = screenType.GetCustomAttribute<ScreenIdAttribute>();
    string id = attr?.Id ?? ScreenIdConvention.ToId(screenType);
    RegisterWithId(id, screenType);
}
```

Also update `GetIdFromType(Type)` static method similarly.

- [ ] **Step 4: Create helper for resolving action IDs**

Add static method (in IBotAction.cs or new file):

```csharp
public static class ActionIdResolver
{
    public static string GetId<TAction>() where TAction : IBotAction =>
        GetId(typeof(TAction));

    public static string GetId(Type actionType)
    {
        var attr = actionType.GetCustomAttribute<ActionIdAttribute>();
        return attr?.Id ?? actionType.Name;
    }
}
```

Update ScreenView.Button<TAction>() and anywhere that uses `typeof(TAction).Name` for callback data.

- [ ] **Step 5: Write tests**

Test that ScreenIdAttribute overrides convention, and ActionIdAttribute overrides type name.

- [ ] **Step 6: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: ActionId and ScreenId attributes for explicit ID override"
```

---

### Task 5: Middleware ordering validation

Add validation in BotApplication.RunAsync() that checks middleware registration order.

**Files:**
- Modify: `src/TelegramBotFlow.Core/Hosting/BotApplication.cs`

- [ ] **Step 1: Track registered middleware types**

Add a list to track which semantic middleware methods were called:

```csharp
private readonly List<string> _registeredMiddleware = [];
```

In each Use* method, add tracking:
```csharp
public BotApplication UseSession()
{
    _registeredMiddleware.Add("session");
    // ... existing registration ...
}
```

- [ ] **Step 2: Add ValidateMiddlewareOrder method**

```csharp
private void ValidateMiddlewareOrder()
{
    int SessionIndex() => _registeredMiddleware.IndexOf("session");
    
    if (_registeredMiddleware.Contains("wizards") && 
        (SessionIndex() < 0 || SessionIndex() > _registeredMiddleware.IndexOf("wizards")))
        throw new InvalidOperationException("UseWizards() requires UseSessions() to be registered before it.");
    
    if (_registeredMiddleware.Contains("pending_input") && 
        (SessionIndex() < 0 || SessionIndex() > _registeredMiddleware.IndexOf("pending_input")))
        throw new InvalidOperationException("UsePendingInput() requires UseSessions() to be registered before it.");
    
    if (_registeredMiddleware.Contains("user_tracking") && 
        (SessionIndex() < 0 || SessionIndex() > _registeredMiddleware.IndexOf("user_tracking")))
        throw new InvalidOperationException("UseUserTracking() requires UseSessions() to be registered before it.");
}
```

- [ ] **Step 3: Call validation in RunAsync**

Add `ValidateMiddlewareOrder()` call at the start of `RunAsync()`.

- [ ] **Step 4: Add UseUserTracking\<TUser\> semantic method**

```csharp
public BotApplication UseUserTracking<TUser>() where TUser : class, IBotUser, new()
{
    _registeredMiddleware.Add("user_tracking");
    return Use<UserTrackingMiddleware<TUser>>();
}
```

- [ ] **Step 5: Write test for validation**

Create `tests/TelegramBotFlow.Core.Tests/Hosting/MiddlewareOrderingTests.cs`:
- Wizards without sessions throws
- Wizards after sessions passes
- PendingInput without sessions throws

- [ ] **Step 6: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: middleware ordering validation at startup"
```

---

### Task 6: Update App example + final verification

Update the App example to demonstrate new APIs. Verify all exit criteria.

**Files:**
- Modify: `src/TelegramBotFlow.App/Program.cs`
- Various App files — update usings for renamed package

- [ ] **Step 1: Update App to use new APIs**

Update Program.cs:
- Replace `Use<UserTrackingMiddleware>()` with `UseUserTracking<BotUser>()`
- Update imports if package was renamed

- [ ] **Step 2: Full build and test**

```bash
dotnet build TelegramBotFlow.slnx -c Release && dotnet test TelegramBotFlow.slnx
```

- [ ] **Step 3: Verify exit criteria**

```bash
# IBotUserStore exists
grep -rn "IBotUserStore" src/TelegramBotFlow.Core.Abstractions/

# ScreenIdAttribute exists
grep -rn "ScreenIdAttribute" src/TelegramBotFlow.Core.Abstractions/

# ActionIdAttribute exists
grep -rn "ActionIdAttribute" src/TelegramBotFlow.Core.Abstractions/

# Webhook secret token
grep -rn "WebhookSecretToken" src/TelegramBotFlow.Core/

# Middleware validation
grep -rn "ValidateMiddlewareOrder" src/TelegramBotFlow.Core/
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "chore: update App example with new APIs, verify exit criteria"
```
