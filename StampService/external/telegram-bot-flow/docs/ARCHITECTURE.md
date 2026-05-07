# Архитектура Telegram Bot Flow (Architecture)

## Обзор (Overview)

Telegram Bot Flow — template-проект для создания Telegram-ботов как тонких клиентов. Бот обрабатывает UI и маршрутизацию, бизнес-логика живёт на бэкенде.

Telegram Bot Flow is a template project for building Telegram bots as thin clients. The bot handles UI and routing; business logic lives on the backend.

## Схема обработки Update (Update Processing Flow)

```
┌─────────────────── Входящие обновления ───────────────────┐
│                  (Incoming updates)                        │
│                                                           │
│  PollingService (BackgroundService)                        │
│  POST /api/bot/webhook                                    │
│                                                           │
└──────────────────────────┬────────────────────────────────┘
                           │ Update
                           ▼
┌─────────────────── Middleware Pipeline ────────────────────┐
│                                                           │
│  ErrorHandlingMiddleware                                   │
│         │                                                 │
│         ▼                                                 │
│  LoggingMiddleware                                         │
│         │                                                 │
│         ▼                                                 │
│  SessionMiddleware  ──────────►  ISessionStore             │
│         │                                                 │
│         ▼                                                 │
│  AccessPolicyMiddleware ────► IUserAccessPolicy           │
│         │                                                 │
│         ▼                                                 │
│  PendingInputMiddleware ───► InputHandlerRegistry          │
│         │                                                 │
│         ▼                                                 │
│  UpdateRouter                                              │
│                                                           │
└──────┬──────────┬──────────┬──────────┬───────────────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
  MapCommand  MapCallback  MapMessage  MapFallback
  (IBotEndpoint classes)
       │          │
       ▼          ▼
  HandlerDelegateFactory
    │
    ▼
  UpdateContext + DI services
  (IUpdateResponder, IScreenNavigator, DbContext, ...)
```

## Проекты (Projects)

### TelegramBotFlow.Core

Framework-часть. При копировании в новый проект эту часть менять не нужно.

The framework layer. When copying to a new project, this part should not be modified.

| Папка / Folder          | Назначение / Purpose                                                                                |
| ----------------------- | --------------------------------------------------------------------------------------------------- |
| `Hosting/`              | `BotApplication`, `BotApplicationBuilder`, `PollingService`, `WebhookEndpoints`, `BotConfiguration` |
| `Pipeline/`             | `UpdateDelegate`, `IUpdateMiddleware`, `UpdatePipeline`                                             |
| `Pipeline/Middlewares/` | `ErrorHandling`, `Logging`, `Session`, `AccessPolicy`, `WizardMiddleware` middleware                |
| `Routing/`              | `UpdateRouter`, `RouteEntry` — маршрутизация по типу обновления / routing by update type            |
| `Context/`              | `UpdateContext`, `IUpdateResponder`, `IUserAccessPolicy`                                            |
| `Sessions/`             | `ISessionStore`, `InMemorySessionStore`, `UserSession`                                              |
| `Screens/`              | `IScreen`, `ScreenManager`, `ScreenNavigator`, `ScreenRegistry`, `ScreenView`                       |
| `Wizards/`              | `BotWizard<T>`, `WizardRegistry`, `WizardMiddleware`, `IWizardStore`, `WizardBuilder`, `StepResult` |
| `UI/`                   | `InlineKeyboard`, `ReplyKeyboard`, `MenuBuilder`                                                    |
| `Endpoints/`            | `IBotEndpoint`, `BotEndpointExtensions` — auto-discovery обработчиков / handler auto-discovery      |
| `Extensions/`           | `ServiceCollectionExtensions` — DI-регистрация / DI registration                                    |

### TelegramBotFlow.Core.Data

EF Core data layer для фреймворка. Управление пользователями бота, отдельный `BotDbContext`.

EF Core data layer for the framework. Bot user management with a dedicated `BotDbContext`.

**Расширяемость / Extensibility:** По аналогии с ASP.NET Identity — `BotUser` можно наследовать, `BotDbContext<TUser>` поддерживает кастомные модели пользователей. Similar to ASP.NET Identity — `BotUser` can be inherited, `BotDbContext<TUser>` supports custom user models.

| Файл / Папка / File / Folder    | Назначение / Purpose                                                                     |
| ------------------------------- | ---------------------------------------------------------------------------------------- |
| `BotUser.cs`                    | Базовая сущность пользователя / Base user entity (`TelegramId`, `JoinedAt`, `IsBlocked`) |
| `BotDbContext.cs`               | Generic `BotDbContext<TUser>` + default `BotDbContext`                                   |
| `Configurations/`               | `BotUserConfiguration` — EF конфигурация / EF configuration (snake_case)                 |
| `Middleware/`                   | `UserTrackingMiddleware<TUser>` + non-generic `UserTrackingMiddleware`                   |
| `Infrastructure/Migrations/`    | EF Core миграция / migration `InitUsers` (таблица / table `users`)                       |
| `DependencyInjectionExtensions` | `AddBotCoreData()` / `AddBotCoreData<TUser, TContext>()`                                 |

### TelegramBotFlow.Broadcasts

Модуль рассылок — отдельный проект со своей БД, Quartz jobs, Telegram-эндпоинтами и REST API.

Broadcast module — a separate project with its own DB, Quartz jobs, Telegram endpoints, and REST API.

**Подход к контенту / Content approach:** Используется Telegram Bot API `copyMessage`. Администратор отправляет сообщение боту (любого типа: текст, фото, видео, документ с форматированием), бот сохраняет `FromChatId` + `MessageId` и при рассылке использует `copyMessage` — сообщение копируется без надписи "Переслано".

Uses the Telegram Bot API `copyMessage`. The admin sends a message to the bot (any type: text, photo, video, document with formatting), the bot stores `FromChatId` + `MessageId`, and during broadcast uses `copyMessage` — the message is copied without the "Forwarded" label.

| Папка / Folder                  | Назначение / Purpose                                                           |
| ------------------------------- | ------------------------------------------------------------------------------ |
| `Domain/`                       | Анемичные модели / Anemic models: `Broadcast`, `BroadcastSequence`, etc.       |
| `Infrastructure/`               | `BroadcastsDbContext`, EF конфигурации / EF configurations (snake_case)        |
| `Infrastructure/Configurations` | `IEntityTypeConfiguration<T>` для broadcast-сущностей / for broadcast entities |
| `Infrastructure/Migrations/`    | EF Core миграции / migrations + Quartz.NET tables                              |
| `Features/Broadcasts/`          | `AdminBroadcastEndpoint` (IBotEndpoint) + REST API (GetAll, Send, Delete)      |
| `Features/Sequences/`           | `AdminSequenceEndpoint` (IBotEndpoint) + REST API (GetAll, Toggle, Delete)     |
| `Features/Users/`               | Просмотр пользователей / User listing (данные из / data from `BotDbContext`)   |
| `Services/`                     | `BroadcastSender` — отправка через / sending via `copyMessage` с rate limiting |
| `Jobs/`                         | `SequenceProcessorJob`, `BroadcastExecutionJob`                                |

**Telegram-эндпоинты / Telegram endpoints (IBotEndpoint):**

- `AdminBroadcastEndpoint` — `/broadcast` команда / command, создание рассылок через Telegram с InlineKeyboard / broadcast creation via Telegram with InlineKeyboard
- `AdminSequenceEndpoint` — `/sequence` сообщает о недоступности Telegram-мастера и направляет в REST API / reports Telegram wizard unavailability and redirects to REST API

**Зависимости / Dependencies:** Core.Data (`BotDbContext`, `BotUser`), SachkovTech.Framework (`IEndpoint`, `EndpointResult`), SachkovTech.SharedKernel (`Error`, `Envelope`), SachkovTech.Core (`CustomValidators`), CSharpFunctionalExtensions (`Result<T, Error>`), Quartz.NET (PostgreSQL persistence), EF Core + Npgsql.

### TelegramBotFlow.App

Точка кастомизации:

The customization entry point:

- `Program.cs` — конфигурация middleware, меню, auto-discovery endpoints, подключение Core.Data и Broadcasts / middleware configuration, menu, auto-discovery endpoints, Core.Data and Broadcasts integration
- `Features/` — фичи, организованные по папкам: хэндлеры, экраны и action-маркеры рядом (см. [CODE_STYLE.md](CODE_STYLE.md)) / features organized by folder: handlers, screens, and action markers co-located (see [CODE_STYLE.md](CODE_STYLE.md))
    - `Features/Start/` — `StartHandler` (`/start`, `/help`)
    - `Features/MainMenu/` — `MainMenuScreen`
    - `Features/Profile/` — `ProfileScreen`
    - `Features/Settings/` — `SettingsScreen`
    - `Features/Help/` — `HelpScreen`
    - `Features/Roadmap/` — `GetRoadmapEndpoint`, `ClearRoadmapEndpoint`, `SetRoadmapInputEndpoint`, `AdminRoadmapScreen`, `SetRoadmapInputScreen`, маркеры `GetRoadmapAction`, `ClearRoadmapAction`, `SetRoadmapInput`
    - `Features/Fallback/` — `FallbackEndpoints`
- Навигация по экранам (`nav:*` callback) встроена во фреймворк — подключается через `app.UseNavigation<MainMenuScreen>()` в `Program.cs` / Screen navigation is built into the framework — enabled via `app.UseNavigation<MainMenuScreen>()` in `Program.cs`
- DI-регистрация backend API клиентов / DI registration of backend API clients
- Swagger UI для управления рассылками / for broadcast management (`/swagger`)

## Ключевые компоненты (Key Components)

### UpdateContext

Контекст update-а в текущей архитектуре — data-only объект:

The update context in the current architecture is a data-only object:

- `Update`, `CancellationToken`
- извлечённые факты / extracted facts: `ChatId`, `UserId`, `MessageId`, `MessageText`, `CallbackData`, `CommandArgument`
- runtime-состояние / runtime state: `Session`, `IsAdmin`

Отправка сообщений и политика доступа вынесены в отдельные сервисы:

Messaging and access policy are extracted into separate services:

- `IUpdateResponder`
- `IUserAccessPolicy`

### Middleware Pipeline

Аналог ASP.NET Core middleware, но для Telegram Update:

ASP.NET Core middleware analog, but for Telegram Updates:

```
ErrorHandling → Logging → Session → AccessPolicy → Wizards → PendingInput → Router → Handler
```

`PendingInputMiddleware` перехватывает текстовые сообщения (не callback, не команды) и роутит их прямо в зарегистрированный input-обработчик, если у сессии установлен `PendingInputActionId`. Это позволяет экранам объявлять «ожидание ввода» через `ScreenView.AwaitInput<TAction>()`.

`PendingInputMiddleware` intercepts text messages (not callbacks, not commands) and routes them directly to the registered input handler if the session has `PendingInputActionId` set. This allows screens to declare "input awaiting" via `ScreenView.AwaitInput<TAction>()`.

Каждый middleware может:

Each middleware can:

- Выполнить логику до/после следующего / Execute logic before/after the next one
- Прервать цепочку (short-circuit) / Short-circuit the chain
- Модифицировать контекст / Modify the context

### Политика доступа (Access Policy — Admin)

`AccessPolicyMiddleware` вычисляет `UpdateContext.IsAdmin` на основе `IUserAccessPolicy`.

`AccessPolicyMiddleware` computes `UpdateContext.IsAdmin` based on `IUserAccessPolicy`.

Стандартная реализация (`BotConfigurationUserAccessPolicy`) использует `Bot:AdminUserIds`.

The default implementation (`BotConfigurationUserAccessPolicy`) uses `Bot:AdminUserIds`.

### Routing и DI в handler-ах (Routing & DI in Handlers)

Minimal API-стиль регистрации:

Minimal API-style registration:

- `MapCommand("/start", handler)` — команды / commands (case-insensitive, поддержка / supports @botname)
- `MapCallback("action:*", handler)` — callback-кнопки с wildcard / callback buttons with wildcard
- `MapAction("callbackId", handler)` — action-кнопка по строковому ID / action button by string ID
- `MapAction<TAction>(handler)` — action-кнопка по типу / action button by type (`typeof(TAction).Name`)
- `MapInput("actionId", handler)` — обработчик свободного текста при активном `PendingInputActionId` / free-text handler when `PendingInputActionId` is active
- `MapInput<TAction>(handler)` — обработчик текста по типу / text handler by type (`typeof(TAction).Name`)
- `MapCallbackGroup("prefix", handler)` — callback по префиксу, action приходит в `string` параметр / callback by prefix, action arrives as `string` parameter
- `MapMessage(predicate, handler)` — текст по предикату / text by predicate
- `MapUpdate(predicate, handler)` — любой тип Update / any Update type

`HandlerDelegateFactory` резолвит параметры handler-а так:

`HandlerDelegateFactory` resolves handler parameters as follows:

- `UpdateContext` — всегда / always
- `CancellationToken` — из контекста / from context
- остальные параметры — из request scope DI / remaining parameters — from request-scoped DI

Первый подходящий маршрут выигрывает.

The first matching route wins.

### IEndpointResult и BotResults (IEndpointResult & BotResults)

Обработчики (action, input, command) возвращают `Task<IEndpointResult>` или `Task` (void):

Handlers (action, input, command) return `Task<IEndpointResult>` or `Task` (void):

```csharp
app.MapAction<ClearAction>(async (UpdateContext ctx, AppDb db) =>
{
    await ClearAsync(db, ctx.CancellationToken);
    return BotResults.Refresh("✅ Удалено");
});

app.MapInput<MessageInput>(async (UpdateContext ctx, AppDb db) =>
{
    await SaveAsync(db, ctx.MessageText, ctx.CancellationToken);
    return BotResults.Back("✅ Сохранено");
});
```

Доступные результаты через `BotResults`:

Available results via `BotResults`:

| Метод / Method                     | Поведение / Behavior                                                                                          |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| `BotResults.ShowView(view)`        | Показывает произвольный `ScreenView` / Shows an arbitrary `ScreenView`                                        |
| `BotResults.Back(msg?)`            | Возврат на предыдущий экран; уведомление опционально / Returns to previous screen; notification is optional   |
| `BotResults.NavigateTo<TScreen>()` | Переход к указанному экрану / Navigates to the specified screen                                               |
| `BotResults.Refresh(msg?)`         | Перерисовка текущего экрана / Redraws the current screen                                                      |
| `BotResults.Stay(msg?)`            | Остаётся в режиме ожидания ввода (`KeepPending = true`) / Stays in input-awaiting mode (`KeepPending = true`) |
| `BotResults.StartWizard<T>()`      | Запускает визард типа `T`, инициализирует первый шаг / Starts wizard of type `T`, initializes first step      |

### Typed Actions (IBotAction) и Payloads

Для типобезопасной связи кнопок с обработчиками используется маркерный интерфейс `IBotAction`:

For type-safe binding of buttons to handlers, the marker interface `IBotAction` is used:

```csharp
public struct ClearRoadmapAction : IBotAction;

// View — кнопка ссылается на тип / button references the type
new ScreenView("...")
    .Button<ClearRoadmapAction>("🗑 Удалить")

// Handler — обработчик ссылается на тот же тип / handler references the same type
app.MapAction<ClearRoadmapAction>(...);
```

#### Типизированные параметры (Typed Payloads)

Также поддерживается передача сложных объектов (payloads). Чтобы обойти лимит Telegram в 64 байта для `callback_data`, используется гибридный подход:

- короткие JSON (≤ 64 байт с учетом префиксов) помещаются напрямую в кнопку.
- длинные JSON кэшируются в сессии (`UserSession.StorePayloadJson` с LRU-очисткой), а в кнопку уходит только 8-символьный Short ID.
- обработчик автоматически десериализует TPayload.

```csharp
// View
new ScreenView("...").Button<DeleteAction, DeletePayload>("🗑", new DeletePayload(Id, true));

// Handler
app.MapAction<DeleteAction, DeletePayload>(async (DeletePayload payload) => ...);
```

Если сессия устарела или очищена, пользователь получит сообщение об ошибке (через перехват `PayloadExpiredException`), а не падение сервиса.

Аналогично для ввода: `AwaitInput<TAction>()` в `ScreenView` + `MapInput<TAction>(handler)` в обработчике. Строковый `ACTION_ID` не нужен.

Similarly for input: `AwaitInput<TAction>()` in `ScreenView` + `MapInput<TAction>(handler)` in the handler. No string `ACTION_ID` needed.

### IBotEndpoint (Auto-Discovery)

Паттерн аналогичный `IEndpoint` в ASP.NET Core Minimal API. Позволяет вынести обработчики из `Program.cs` в отдельные классы:

A pattern similar to `IEndpoint` in ASP.NET Core Minimal API. Allows extracting handlers from `Program.cs` into separate classes:

```csharp
public sealed class StartCommandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    app.MapCommand("/start", async (UpdateContext ctx, IUpdateResponder responder) =>
        {
      await responder.ReplyAsync(ctx, "Привет!");
        });
    }
}
```

Регистрация в `Program.cs`:

Registration in `Program.cs`:

```csharp
BotApplicationBuilder builder = BotApplication.CreateBuilder(args);
BotApplication app = builder.Build();
app.MapBotEndpoints();
```

`builder.Build()` автоматически вызывает `AddBotEndpoints` и `AddScreens` для entry assembly.
`MapBotEndpoints` резолвит их и вызывает `MapEndpoint` для каждого.

`builder.Build()` automatically calls `AddBotEndpoints` and `AddScreens` for the entry assembly.
`MapBotEndpoints` resolves them and calls `MapEndpoint` for each one.

### Wizards (Мастера ввода)

Система визардов позволяет собирать данные от пользователя в несколько шагов. Паттерн регистрации идентичен `ScreenRegistry`.

The wizard system collects user data across multiple steps. The registration pattern mirrors `ScreenRegistry`.

```
WizardRegistry (Singleton) → string wizardId → Type wizardType
    ↓ on each update (only when wizard is active)
context.RequestServices.GetRequiredService(wizardType) → IBotWizard (Scoped)
```

**Ключевые компоненты / Key components:**

- `WizardRegistry` — Singleton-реестр, хранит `string → Type`. Не создаёт экземпляры. / Singleton registry, stores `string → Type`. Does not instantiate wizards.
- `BotWizard<TState>` — базовый класс визарда. Шаги описываются через `ConfigureSteps(WizardBuilder)`. / Base wizard class. Steps are described via `ConfigureSteps(WizardBuilder)`.
- `IWizardStore` — хранилище состояния прохождения визарда. По умолчанию `InMemoryWizardStore`. / Wizard progress state store. Default: `InMemoryWizardStore`.
- `WizardMiddleware` — перехватывает апдейт при активном `session.ActiveWizardId`. Резолвит визард из реестра. / Intercepts the update when `session.ActiveWizardId` is set. Resolves the wizard from the registry.

**Запуск визарда / Starting a wizard:**

```csharp
// Возврат IEndpointResult — предпочтительный способ
app.MapCommand("/register", () => BotResults.StartWizard<RegistrationWizard>());

// Или императивно из обработчика (когда нужна логика до запуска)
app.MapCommand("/register", async (UpdateContext ctx) =>
{
    await ctx.StartWizardAsync<RegistrationWizard>(ctx.CancellationToken);
    return BotResults.Empty();
});
```

**Регистрация / Registration:**

```csharp
// Автоматически из builder.Build()
services.AddWizards(entryAssembly);

// Или вручную
builder.Services.AddWizards(typeof(RegistrationWizard).Assembly);
```

**Пример визарда / Wizard example:**

```csharp
public class RegistrationWizard : BotWizard<RegistrationState>
{
    protected override void ConfigureSteps(WizardBuilder<RegistrationState> builder)
    {
        builder
            .Step("name",
                renderer: (ctx, state) => new ScreenView("Введите имя:"),
                processor: async (ctx, state) =>
                {
                    state.Name = ctx.MessageText!;
                    return StepResult.GoTo("age");
                })
            .Step("age",
                renderer: (ctx, state) => new ScreenView("Введите возраст:"),
                processor: async (ctx, state) =>
                {
                    if (!int.TryParse(ctx.MessageText, out int age))
                        return StepResult.Stay("Введите число");
                    state.Age = age;
                    return StepResult.Finish();
                });
    }

    public override async Task<IEndpointResult> OnFinishedAsync(UpdateContext ctx, RegistrationState state)
    {
        // Сохранить данные, перейти на экран
        return BotResults.NavigateToRoot<MainMenuScreen>();
    }
}
```

### Screens и навигация (Screens & Navigation)

UI-диалоги реализованы через экраны:

UI dialogs are implemented via screens:

1. `IScreen.RenderAsync` возвращает / returns `ScreenView`
2. `ScreenManager` рендерит экран и обновляет навигационные поля сессии / renders the screen and updates session navigation fields
3. `IScreenNavigator` управляет / manages `NavigateTo`, `GoBack`, `RefreshScreen`
4. `ScreenRegistry` регистрирует экраны по конвенции snake_case из имени класса / registers screens by snake_case convention from class name

`ScreenView` — rich builder для содержимого экрана: поддерживает InlineKeyboard, медиа (фото, видео, документ, анимация), навигационные кнопки (`NavigateButton<T>`, `BackButton`, `CloseButton`, `MenuButton`). Кнопки навигации генерируют callback `nav:{screenId}` и обрабатываются встроенным навигационным обработчиком (подключается через `UseNavigation<TMenuScreen>()`).

`ScreenView` is a rich builder for screen content: supports InlineKeyboard, media (photo, video, document, animation), navigation buttons (`NavigateButton<T>`, `BackButton`, `CloseButton`, `MenuButton`). Navigation buttons generate callback `nav:{screenId}` and are handled by the built-in navigation handler (enabled via `UseNavigation<TMenuScreen>()`).

Системные callback-ID навигации вынесены в класс `NavCallbacks` (константы `BACK`, `CLOSE`, `MENU`). Используй эти константы вместо raw строк `"nav:back"` / `"nav:close"` / `"nav:menu"` везде, где нужно сослаться на навигационный callback вручную (например, при ручном построении `InlineKeyboardMarkup`).

System navigation callback IDs are defined in the `NavCallbacks` class (constants `BACK`, `CLOSE`, `MENU`). Use these constants instead of raw strings `"nav:back"` / `"nav:close"` / `"nav:menu"` wherever you need to reference a navigation callback manually (e.g. when building `InlineKeyboardMarkup` by hand).

#### Разница между GoBackAsync и NavigateBackAsync

| Метод               | Стек пуст                      | Callback-ответ                      |
| ------------------- | ------------------------------ | ----------------------------------- |
| `GoBackAsync`       | no-op (молчаливый)             | нет                                 |
| `NavigateBackAsync` | перерисовывает `CurrentScreen` | да (опциональный текст уведомления) |

`GoBackAsync` используется в программном коде. `NavigateBackAsync` — в обработчиках callback-кнопок (`nav:back`), где нужно ответить на callback-запрос Telegram.

### Sessions (Сессии)

- `ISessionStore` — абстракция хранилища / storage abstraction
- `InMemorySessionStore` — default (ConcurrentDictionary)
- `RedisSessionStore` — production-ready хранилище сессий с поддержкой TTL / production-ready session store with TTL support
- `UserSession` — key-value хранилище + состояние навигации экранов / key-value store + screen navigation state

`UserSession` хранит навигационное состояние (`CurrentScreen`, `NavMessageId`, `NavigationStack`, `PendingInputActionId`). Setters этих полей помечены `internal` — внешний код должен использовать методы `PushScreen`, `PopScreen`, `Clear`, `ResetNavigation`, `SetPending`.

`UserSession` stores navigation state (`CurrentScreen`, `NavMessageId`, `NavigationStack`, `PendingInputActionId`). Setters of these fields are `internal` — external code should use the methods `PushScreen`, `PopScreen`, `Clear`, `ResetNavigation`, `SetPending`.

## Режимы работы (Operating Modes)

| Режим / Mode | Компонент / Component                | Когда использовать / When to use                             |
| ------------ | ------------------------------------ | ------------------------------------------------------------ |
| Polling      | `PollingService` (BackgroundService) | Разработка, нет публичного URL / Development, no public URL  |
| Webhook      | Minimal API endpoint + `SetWebhook`  | Production, есть HTTPS URL / Production, HTTPS URL available |

Переключение через `Bot:Mode` в `appsettings.json`.

Switch via `Bot:Mode` in `appsettings.json`.

## Стиль кода (Code Style)

Проект использует `SCREAMING_CASE` для констант и значений enum. Подробности в [CODE_STYLE.md](CODE_STYLE.md).

The project uses `SCREAMING_CASE` for constants and enum values. See [CODE_STYLE.md](CODE_STYLE.md) for details.

## Практическая конфигурация App (Practical App Configuration)

Базовый `Program.cs` в приложении обычно собирается так:

A typical `Program.cs` in the application is composed as follows:

```csharp
BotApplication app = builder.Build();

app.UseErrorHandling();
app.UseLogging();
app.UseSession();
app.UseAccessPolicy();
app.Use<UserTrackingMiddleware>();
app.UsePendingInput();

app.SetMenu(menu => menu
  .Command("start", "Главное меню"));

app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();
await app.RunAsync();
```
