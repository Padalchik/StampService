# Phase 1: Cleanup — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove all unnecessary code and dependencies so the project compiles cleanly with zero external private packages.

**Architecture:** Delete the Broadcasts module entirely, remove SachkovTech.* NuGet references from Directory.Packages.props, unify namespaces in Core.Abstractions, remove bot-specific dead code (BotSettings/Roadmap) from Core.Data, and clean the App example. Each task produces a compiling, test-passing state.

**Tech Stack:** .NET 10, Telegram.Bot 22.9.0, EF Core 10, xUnit

---

### Task 1: Remove Broadcasts module from solution

**Files:**
- Modify: `TelegramBotFlow.slnx`
- Delete: `src/TelegramBotFlow.Broadcasts/` (entire directory)

- [ ] **Step 1: Remove Broadcasts project from solution file**

Edit `TelegramBotFlow.slnx` — remove the Broadcasts project line:

```xml
<!-- REMOVE this line: -->
<Project Path="src/TelegramBotFlow.Broadcasts/TelegramBotFlow.Broadcasts.csproj" />
```

The file should look like:
```xml
<Solution>
  <Configurations>
    <Platform Name="Any CPU" />
    <Platform Name="x64" />
    <Platform Name="x86" />
  </Configurations>
  <Folder Name="/src/">
    <Project Path="src/TelegramBotFlow.App/TelegramBotFlow.App.csproj" />
    <Project Path="src/TelegramBotFlow.Core.Abstractions/TelegramBotFlow.Core.Abstractions.csproj" />
    <Project Path="src/TelegramBotFlow.Core.Data/TelegramBotFlow.Core.Data.csproj" />
    <Project Path="src/TelegramBotFlow.Core.Redis/TelegramBotFlow.Core.Redis.csproj" />
    <Project Path="src/TelegramBotFlow.Core/TelegramBotFlow.Core.csproj" />
  </Folder>
  <Folder Name="/tests/">
    <Project Path="tests/TelegramBotFlow.Core.Tests/TelegramBotFlow.Core.Tests.csproj" />
    <Project Path="tests/TelegramBotFlow.IntegrationTests/TelegramBotFlow.IntegrationTests.csproj" />
  </Folder>
</Solution>
```

- [ ] **Step 2: Delete the Broadcasts directory**

```bash
rm -rf "src/TelegramBotFlow.Broadcasts"
```

- [ ] **Step 3: Verify solution builds**

```bash
cd "/Users/dev/code/miracle generation/telegram-bot-flow"
dotnet build TelegramBotFlow.slnx
```

Expected: Build succeeded (Broadcasts had no inbound references from other projects).

- [ ] **Step 4: Verify tests pass**

```bash
dotnet test TelegramBotFlow.slnx
```

Expected: All existing tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: remove Broadcasts module from solution"
```

---

### Task 2: Remove SachkovTech.* and unused packages from Directory.Packages.props

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Remove SachkovTech packages and Broadcasts-only dependencies**

In `Directory.Packages.props`, remove these lines:

```xml
<!-- REMOVE: SachkovTech Packages -->
<PackageVersion Include="SachkovTech.SharedKernel" Version="0.1.3" />
<PackageVersion Include="SachkovTech.Core" Version="0.1.4" />
<PackageVersion Include="SachkovTech.Framework" Version="0.1.2" />
```

```xml
<!-- REMOVE: Quartz.NET (only used by Broadcasts) -->
<PackageVersion Include="Quartz" Version="3.15.1" />
<PackageVersion Include="Quartz.Extensions.Hosting" Version="3.15.1" />
<PackageVersion Include="Quartz.Serialization.SystemTextJson" Version="3.15.1" />
```

```xml
<!-- REMOVE: CSharpFunctionalExtensions (only used by Broadcasts) -->
<PackageVersion Include="CSharpFunctionalExtensions" Version="3.6.0" />
```

```xml
<!-- REMOVE: FluentValidation (only used by Broadcasts) -->
<PackageVersion Include="FluentValidation" Version="12.1.1" />
```

```xml
<!-- REMOVE: Swagger (only used by Broadcasts REST endpoints) -->
<PackageVersion Include="Swashbuckle.AspNetCore.SwaggerGen" Version="10.1.1" />
<PackageVersion Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.1.1" />
```

- [ ] **Step 2: Verify no remaining .csproj references SachkovTech**

```bash
grep -r "SachkovTech" --include="*.csproj" src/ tests/
```

Expected: Only `<Authors>SachkovTech</Authors>` and `<Company>SachkovTech</Company>` metadata — no `PackageReference`. These author metadata lines are fine to keep.

- [ ] **Step 3: Verify no .cs files reference SachkovTech**

```bash
grep -r "using SachkovTech" --include="*.cs" src/ tests/
```

Expected: No matches.

- [ ] **Step 4: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: Build succeeded, all tests pass.

- [ ] **Step 5: Commit**

```bash
git add Directory.Packages.props
git commit -m "chore: remove SachkovTech and Broadcasts-only packages"
```

---

### Task 3: Fix namespace inconsistency in Core.Abstractions

Three files use `TelegramBotFlow.Core.Abstractions.*` namespace while the other 29 use `TelegramBotFlow.Core.*`. Unify to `TelegramBotFlow.Core.*`.

**Files:**
- Modify: `src/TelegramBotFlow.Core.Abstractions/Routing/BotResults.cs`
- Modify: `src/TelegramBotFlow.Core.Abstractions/Routing/IEndpointResult.cs`
- Modify: `src/TelegramBotFlow.Core.Abstractions/Screens/INavigationService.cs`

- [ ] **Step 1: Fix BotResults.cs namespace**

In `src/TelegramBotFlow.Core.Abstractions/Routing/BotResults.cs`, change:

```csharp
namespace TelegramBotFlow.Core.Abstractions.Routing;
```

to:

```csharp
namespace TelegramBotFlow.Core.Routing;
```

- [ ] **Step 2: Fix IEndpointResult.cs namespace**

In `src/TelegramBotFlow.Core.Abstractions/Routing/IEndpointResult.cs`, change:

```csharp
namespace TelegramBotFlow.Core.Abstractions.Routing;
```

to:

```csharp
namespace TelegramBotFlow.Core.Routing;
```

- [ ] **Step 3: Fix INavigationService.cs namespace**

In `src/TelegramBotFlow.Core.Abstractions/Screens/INavigationService.cs`, change:

```csharp
namespace TelegramBotFlow.Core.Abstractions.Screens;
```

to:

```csharp
namespace TelegramBotFlow.Core.Screens;
```

- [ ] **Step 4: Update all consumer `using` statements**

Search for `using TelegramBotFlow.Core.Abstractions.Routing` and `using TelegramBotFlow.Core.Abstractions.Screens` across the entire codebase and replace:

```bash
grep -rn "using TelegramBotFlow.Core.Abstractions\." --include="*.cs" src/ tests/
```

For every match, replace:
- `using TelegramBotFlow.Core.Abstractions.Routing;` → `using TelegramBotFlow.Core.Routing;`
- `using TelegramBotFlow.Core.Abstractions.Screens;` → `using TelegramBotFlow.Core.Screens;`

Known files that reference these namespaces (from App example):
- `src/TelegramBotFlow.App/Features/Roadmap/GetRoadmapAction.cs` — line 3: `using TelegramBotFlow.Core.Abstractions.Routing;`
- `src/TelegramBotFlow.App/Features/Roadmap/SetRoadmapInput.cs` — line 4: `using TelegramBotFlow.Core.Abstractions.Routing;`
- `src/TelegramBotFlow.App/Features/Roadmap/ClearRoadmapAction.cs` — line 1: `using TelegramBotFlow.Core.Abstractions.Routing;`

Replace all with `using TelegramBotFlow.Core.Routing;`.

- [ ] **Step 5: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: Build succeeded, all tests pass. If any file has a missing `using`, the build error will name the exact file and type — add the correct `using TelegramBotFlow.Core.Routing;` or `using TelegramBotFlow.Core.Screens;`.

- [ ] **Step 6: Verify no Abstractions namespace remains**

```bash
grep -rn "TelegramBotFlow.Core.Abstractions\." --include="*.cs" src/ tests/
```

Expected: No matches.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: unify Core.Abstractions namespaces to TelegramBotFlow.Core.*"
```

---

### Task 4: Remove BotSettings and Roadmap dead code from Core.Data

BotSettings, RoadmapMessageConfig, and BotSettingsConfiguration are bot-specific — they belong in the App example, not the framework. However, they are also referenced in the migration. Since migrations are immutable, we create a new migration that drops the `bot_settings` table.

**Files:**
- Delete: `src/TelegramBotFlow.Core.Data/BotSettings.cs`
- Delete: `src/TelegramBotFlow.Core.Data/Configurations/BotSettingsConfiguration.cs`
- Modify: `src/TelegramBotFlow.Core.Data/BotDbContext.cs` (remove Settings DbSet)
- Modify: `src/TelegramBotFlow.Core.Data/DependencyInjectionExtensions.cs` (no changes needed — BotSettings not referenced here)

- [ ] **Step 1: Remove Settings DbSet from BotDbContext**

In `src/TelegramBotFlow.Core.Data/BotDbContext.cs`, remove the `Settings` property. File becomes:

```csharp
using Microsoft.EntityFrameworkCore;

namespace TelegramBotFlow.Core.Data;

/// <summary>
/// Generic DbContext for bot user storage.
/// Inherit to add custom user properties (ASP.NET Identity pattern):
/// <code>
/// public class AppDbContext : BotDbContext&lt;AppUser&gt; { }
/// </code>
/// </summary>
public class BotDbContext<TUser>(DbContextOptions options)
    : DbContext(options) where TUser : BotUser, new()
{
    public DbSet<TUser> Users => Set<TUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotDbContext<>).Assembly);
    }
}

/// <summary>
/// Default non-generic BotDbContext for simple bots that use BotUser as-is.
/// </summary>
public class BotDbContext(DbContextOptions<BotDbContext> options)
    : BotDbContext<BotUser>(options);
```

- [ ] **Step 2: Delete BotSettings.cs**

```bash
rm "src/TelegramBotFlow.Core.Data/BotSettings.cs"
```

- [ ] **Step 3: Delete BotSettingsConfiguration.cs**

```bash
rm "src/TelegramBotFlow.Core.Data/Configurations/BotSettingsConfiguration.cs"
```

- [ ] **Step 4: Generate a corrective migration to drop bot_settings table**

```bash
cd "/Users/dev/code/miracle generation/telegram-bot-flow"
dotnet ef migrations add RemoveBotSettings \
  --project src/TelegramBotFlow.Core.Data \
  --startup-project src/TelegramBotFlow.App
```

This will generate a migration that drops the `bot_settings` table. Verify the generated `Up()` method contains `migrationBuilder.DropTable(name: "bot_settings")` and `Down()` recreates it.

- [ ] **Step 5: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: Build may fail because App's Roadmap feature references BotSettings. That's fixed in Task 5.

- [ ] **Step 6: Commit (partial — will complete after Task 5)**

Do NOT commit yet — wait until App example is cleaned in Task 5.

---

### Task 5: Clean App example — remove Roadmap feature and BotSettings references

The Roadmap feature is bot-specific (uses BotSettings, StorageChannelId). Remove it entirely from the App example. Update MainMenuScreen to remove Roadmap buttons.

**Files:**
- Delete: `src/TelegramBotFlow.App/Features/Roadmap/` (entire directory — 5 files)
- Modify: `src/TelegramBotFlow.App/Features/MainMenu/MainMenuScreen.cs`

- [ ] **Step 1: Delete the Roadmap feature directory**

```bash
rm -rf "src/TelegramBotFlow.App/Features/Roadmap"
```

- [ ] **Step 2: Update MainMenuScreen to remove Roadmap references**

Replace `src/TelegramBotFlow.App/Features/MainMenu/MainMenuScreen.cs` with:

```csharp
using TelegramBotFlow.App.Features.Feedback;
using TelegramBotFlow.App.Features.Help;
using TelegramBotFlow.App.Features.Onboarding;
using TelegramBotFlow.App.Features.Profile;
using TelegramBotFlow.App.Features.Settings;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.App.Features.MainMenu;

/// <summary>
/// Main menu screen with navigation to key sections.
/// </summary>
public sealed class MainMenuScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        ScreenView view = new ScreenView("Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row()
            .Button<StartProfileSetupAction>("✏️ Настроить профиль")
            .Button<StartFeedbackAction>("💬 Оставить отзыв");

        return ValueTask.FromResult(view);
    }
}
```

- [ ] **Step 3: Remove StorageChannelId from BotConfiguration if it's only used by Roadmap**

Check `src/TelegramBotFlow.Core/Hosting/BotConfiguration.cs`. The `StorageChannelId` property is a generic framework feature (copy messages to storage channel) — keep it. It may be useful for other bots.

- [ ] **Step 4: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: Build succeeded, all tests pass.

- [ ] **Step 5: Commit Task 4 + Task 5 together**

```bash
git add -A
git commit -m "chore: remove BotSettings, Roadmap feature, and bot_settings table

BotSettings/RoadmapMessageConfig are bot-specific, not framework code.
Corrective migration drops the bot_settings table.
Roadmap feature removed from App example."
```

---

### Task 6: Remove commented-out code and clean unused usings

**Files:**
- Scan all `.cs` files in `src/` for commented-out code blocks

- [ ] **Step 1: Search for commented-out code blocks**

```bash
grep -rn "^\s*//.*=\|^\s*//.*await\|^\s*//.*return\|^\s*//.*var " --include="*.cs" src/
```

Review each match. Remove any commented-out code that is dead (not documentation/explanation). Keep `//` comments that explain WHY something works a certain way.

- [ ] **Step 2: Clean unused usings across the solution**

```bash
dotnet format TelegramBotFlow.slnx --diagnostics IDE0005 --severity info
```

Or manually inspect build warnings for unused usings.

- [ ] **Step 3: Build and test**

```bash
dotnet build TelegramBotFlow.slnx && dotnet test TelegramBotFlow.slnx
```

Expected: Build succeeded, all tests pass, fewer warnings.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: remove commented-out code and unused usings"
```

---

### Task 7: Final verification — exit criteria check

- [ ] **Step 1: Full build**

```bash
cd "/Users/dev/code/miracle generation/telegram-bot-flow"
dotnet build TelegramBotFlow.slnx -c Release
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 2: Full test run**

```bash
dotnet test TelegramBotFlow.slnx
```

Expected: All tests pass.

- [ ] **Step 3: Verify zero SachkovTech references**

```bash
grep -rn "SachkovTech" --include="*.cs" src/ tests/
```

Expected: No matches.

```bash
grep -rn "SachkovTech\.\(SharedKernel\|Core\|Framework\)" --include="*.csproj" src/ tests/
```

Expected: No PackageReference matches (only Author/Company metadata is OK).

- [ ] **Step 4: Verify zero Broadcasts references**

```bash
grep -rn "Broadcast" --include="*.cs" src/ tests/
grep -rn "Broadcasts" --include="*.csproj" src/ tests/
grep -rn "Broadcasts" --include="*.slnx" .
```

Expected: No matches.

- [ ] **Step 5: Verify namespace consistency**

```bash
grep -rn "TelegramBotFlow.Core.Abstractions\." --include="*.cs" src/ tests/
```

Expected: No matches — all types in Core.Abstractions use `TelegramBotFlow.Core.*` namespace.

- [ ] **Step 6: Verify no BotSettings references remain**

```bash
grep -rn "BotSettings\|RoadmapMessageConfig" --include="*.cs" src/ tests/
```

Expected: No matches (migration files in `obj/` are OK — they're generated and don't affect compilation).

- [ ] **Step 7: Tag Phase 1 complete**

No git tag — just verify all checks pass. Phase 1 is done.
