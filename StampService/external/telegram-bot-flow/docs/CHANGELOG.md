# Журнал изменений (Changelog)

Все значимые изменения проекта документируются в этом файле.

All notable changes to this project are documented in this file.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.0.0] - 2026-04-16

### Breaking Changes
- **Middleware registration moved to BotApplicationBuilder.** All `app.Use*()` calls are now `builder.Use*()`, called before `builder.Build()`.
- **`BotConfiguration.ErrorMessage` removed** — use `BotMessages.ErrorMessage` via Options pattern.

### Added
- `BotMessages` class for localizable framework strings (BackButton, MenuButton, CloseButton, PayloadExpired, ErrorMessage)
- Configurable parameters in `BotConfiguration`: PayloadCacheSize, SessionLockTimeoutSeconds, MaxConcurrentUpdates, MaxNavigationDepth, UpdateChannelCapacity, WizardDefaultTtlMinutes, ShutdownTimeoutSeconds, TelegramRateLimitPerSecond, MaxRetryOnRateLimit, HealthCheckPath
- `UpdateContext.User` (IBotUser?) — set by UserTrackingMiddleware
- `UpdateContext.HandlerName` (string?) — set by router for structured logging
- Typed input properties on UpdateContext: Photos, Document, Contact, Location, Voice, VideoNote, Video, HasMedia
- Reply Keyboard support in ScreenView: `WithReplyKeyboard()`, `RemoveReplyKeyboard()`
- `IBotNotifier` — proactive messaging outside the pipeline
- `IBotBroadcaster` — batch messaging with error tracking, blocked user detection, configurable concurrency
- Telegram API rate limiting via `TokenBucketRateLimiter` (System.Threading.RateLimiting)
- HTTP retry policy via `Microsoft.Extensions.Http.Resilience` (Polly v8) for 429/500/503
- `MapDeepLink()` — deep link routing with HIGH priority
- `MapChatMember()` — route MyChatMember updates
- `UseWhen()` — conditional middleware execution
- `BotWizard<TState>.OnCancelledAsync()` — cleanup callback on wizard cancellation
- Health check endpoint (configurable path, default `/health`)
- Graceful shutdown timeout configuration
- Structured error logging with UpdateType, UserId, Screen, HandlerName
- ChatMemberUpdated in default AllowedUpdates — auto-block on Kicked status
- `.claude/` AI configuration (settings, agents, rules, memory)

### Fixed
- InMemoryWizardStore memory leak — replaced ConcurrentDictionary with IMemoryCache
- Hardcoded Russian strings replaced with English defaults

### Changed
- Default button text: "← Назад" → "← Back", "☰ Главное меню" → "☰ Menu"
- PayloadExpiredException message changed to English
- InMemorySessionLockProvider reads timeout from BotConfiguration
- UpdateProcessingWorker reads concurrency from BotConfiguration
- Update channel capacity reads from BotConfiguration

### Tests
- +60 new tests (374 total: 309 unit + 65 integration)
- EfBotUserStore integration tests
- UserTrackingMiddleware unit tests
- Concurrent session stress tests
- TelegramRateLimitHandler tests
- BotNotifier and BotBroadcaster tests
- DeepLink and ChatMember routing tests
- UseWhen conditional middleware tests
- Wizard cancellation tests
- InMemoryWizardStore tests
- PendingInputMiddleware photo input test
- Reply Keyboard tests

## [Unreleased]

### Добавлено (Added)

- **`WizardRegistry`** — новый Singleton-реестр визардов, аналог `ScreenRegistry`. Хранит соответствие `string → Type`. Экземпляр визарда создаётся из DI on-demand только при обработке апдейта активного визарда. Устранён паттерн `IEnumerable<IBotWizard>` и связанный с ним баг молчаливого игнорирования всех визардов кроме первого (`TryAddScoped(typeof(IBotWizard), ...)` заменён на `services.AddScoped(concreteType)`).
- **`BotResults.StartWizard<T>()`** — новый `IEndpointResult` для запуска визарда из обработчика. Позволяет писать `app.MapCommand("/reg", () => BotResults.StartWizard<RegistrationWizard>())` вместо ручного вызова `ctx.StartWizardAsync<T>()` + возврата `BotResults.Empty()`.

### Изменено (Changed)

- **`WizardMiddleware`** — заменён `IEnumerable<IBotWizard>` на `WizardRegistry`. Middleware больше не создаёт все зарегистрированные визарды при каждом апдейте.
- **`WizardContextExtensions.StartWizardAsync`** — использует `WizardRegistry` вместо `IEnumerable<IBotWizard>`.

### Удалено (Removed)

- **`WizardState<TState>`** — удалён неиспользуемый дженерик-класс. Для хранения используется `WizardStorageState` (JSON-сериализация payload).

### Добавлено (Added)

- **NavigateToRoot** — результаты `BotResults.NavigateToRoot<TScreen>()` и `BotResults.NavigateToRoot(screenId)`: переход на экран с полной очисткой истории навигации (стек сбрасывается). Удобно после завершения визарда или критичного действия.
- **Аргументы перехода (Navigation args)** — в `UserSession`: `SetNavigationArg(key, value)` / `SetNavigationArg<T>(key, value)` перед переходом; в целевом экране в `RenderAsync` — `GetNavigationArg(key)` / `GetNavigationArg<T>(key)`. Аргументы автоматически очищаются после отрисовки экрана. Позволяет передавать параметры на экран без костылей с общим state.
- **StayResult с опцией удаления сообщения** — `BotResults.Stay(notification, deleteMessage: true)` (по умолчанию удаляет сообщение пользователя); `deleteMessage: false` — только ответить на callback, не удаляя ввод.
- **Документация** — в `docs/USAGE.md`: конвенция имён экранов (суффикс `Screen`), раздел 3.4 «Параметры перехода», таблица BotResults с NavigateToRoot и Stay(deleteMessage), пояснения Back vs GoBackAsync, Refresh (no-op при отсутствии CurrentScreen), обновлён чеклист фич.

### Добавлено (Added) — ранее

- **Типизированные параметры для кнопок (Typed Payloads)** — поддержка передачи типизированных объектов через `ScreenView.Button<TAction, TPayload>(...)`. Ограничение Telegram в 64 байта для `callback_data` обходится гибридным подходом: если JSON помещается (длина <= 64 байт), он встраивается прямо в кнопку (`TAction:j:{json}`). Если нет, JSON кэшируется в `UserSession` (LRU-буфер на 500 записей), а в кнопку кладётся Short ID (`TAction:s:{ShortId}`). В обработчиках достаточно добавить параметр `TPayload`, парсинг и десериализация из сессии выполняются автоматически в `HandlerDelegateFactory`.
- `PayloadExpiredException` — если пейлоад из сессии был удален (или устарел), кнопка не вызывает падение приложения. Вместо этого пользователь видит нативный Telegram Alert ("Данные кнопки устарели. Пожалуйста, обновите меню.").

### Исправлено (Fixed)

- **`SessionMiddleware`** — утечка памяти: семафоры в `_userLocks` теперь удаляются из `ConcurrentDictionary` после освобождения (cleanup через `TryRemove` с проверкой конкретной пары ключ-значение).
- **`UpdateResponder.ReplaceAnchorWithCopyAsync`** — пустой `catch {}` заменён на `catch (ApiRequestException ex) when (ex.ErrorCode is 400 or 403)`. Неожиданные ошибки (сеть, таймаут) больше не проглатываются молча.

### Добавлено (Added)

- **`NavCallbacks`** — новый статический класс с константами системных навигационных callback-ов: `BACK = "nav:back"`, `CLOSE = "nav:close"`, `MENU = "nav:menu"`. Используй вместо raw строк при ручном построении `InlineKeyboardMarkup`.

### Изменено (Changed)

- **`BotApplication.UseXxx()`** — `UseErrorHandling()`, `UseLogging()`, `UseSession()`, `UseAccessPolicy()`, `UsePendingInput()` упрощены: теперь делегируют в `Use<TMiddleware>()` вместо дублирования idентичного boilerplate-кода.
- **`UserSession`** — навигационные поля (`CurrentScreen`, `NavMessageId`, `CurrentMediaType`, `NavigationStack`, `PendingInputActionId`) получили `internal set`. Изменение из внешнего кода (за пределами `TelegramBotFlow.Core`) больше невозможно — используй методы `PushScreen`, `PopScreen`, `Clear`, `ResetNavigation`, `SetPending`.
- **`IScreenNavigator`** — улучшены docstrings `GoBackAsync` и `NavigateBackAsync`: явно описано поведение при пустом стеке навигации и разница в обработке callback-ответа.
- **`ScreenView`** — внутренние строки навигационных кнопок (`BackButton`, `CloseButton`, `MenuButton`) заменены на константы `NavCallbacks`.
- **`GetRoadmapAction`** — raw строка `"nav:menu"` заменена на `NavCallbacks.MENU`.
- **`ARCHITECTURE.md`** — добавлена `PendingInputMiddleware` в схему pipeline; добавлены секции про `NavCallbacks`, разницу `GoBackAsync` vs `NavigateBackAsync`, обновлена секция Sessions.
- **`CODE_STYLE.md`** — добавлен раздел про `NavCallbacks` и разрешение `db.SaveChangesAsync()` в App-слое.

### Изменено (Changed) — предыдущие изменения

- `RedisSessionStore` — переход с Redis Hash (`HSET`/`HGETALL`) на единый JSON-payload в Redis String (`GET`/`SET`). Устранены ручные маппинги полей (`HashEntry[]`, `TryParseDate`, `NullIfEmpty`). Внутренние методы переименованы: `Serialize` → `ToJson`, `Deserialize` → `FromJson`. Null-поля не сериализуются (camelCase + `WhenWritingNull`).

### Добавлено (Added)

- `IEndpointResult` — единый интерфейс результата обработчика (unified handler result interface, similar to `IActionResult` in ASP.NET Core):
    - `ShowViewResult(ScreenView)` — показывает произвольное представление (shows an arbitrary view)
    - `NavigateBackResult(notification?)` — якорный возврат на предыдущий экран (navigates back to previous screen)
    - `NavigateToResult(Type)` — переход к экрану по типу (navigates to screen by type)
    - `StayResult(notification?)` — остаётся в режиме ожидания ввода (stays in input-awaiting mode, `KeepPending = true`)
    - `RefreshResult(notification?)` — перерисовывает текущий экран без изменения стека (refreshes current screen without changing stack)
- `BotResults` — статическая фабрика результатов (static result factory, similar to `Results` in ASP.NET Core Minimal APIs):
    - `BotResults.ShowView(view)`, `Back(notification?)`, `Stay(notification?)`, `NavigateTo<T>()`, `Refresh(notification?)`
- `IBotAction` — маркерный интерфейс для типизированных точек взаимодействия (marker interface for typed interaction points — buttons & input)
- `ScreenView.Button<TAction>(text)` — типизированная кнопка действия (typed action button); callback ID из `typeof(TAction).Name`
- `ScreenView.AwaitInput<TAction>()` — типизированная версия `AwaitInput` (typed `AwaitInput`); action ID из `typeof(TAction).Name`
- `BotApplication.MapAction<TAction>(handler)` — типизированная регистрация action-обработчика (typed action handler registration)
- `BotApplication.MapInput<TAction>(handler)` — типизированная регистрация input-обработчика (typed input handler registration)
- Полное XML-документирование (complete XML documentation: `/// <summary>`, `param`, `returns`) для ключевых классов и методов в `TelegramBotFlow.App` и `TelegramBotFlow.Core`
- `BotApplication.UseNavigation<TMenuScreen>()` — встроенная навигация по экранам (callback `nav:*`), отдельный обработчик в App не требуется (built-in screen navigation for `nav:*` callbacks; no separate handler in App required)

### Изменено (Changed)

- `HandlerDelegateFactory` упрощён (simplified): удалён `CreateForAction`, единый `DispatchAsync` для всех путей (removed `CreateForAction`, unified `DispatchAsync` for all paths)
- `MapAction` теперь принимает `Task` (void) и `Task<IEndpointResult>` вместо `Task<ScreenView>` (now accepts `Task` (void) and `Task<IEndpointResult>` instead of `Task<ScreenView>`)
- `MapInput` теперь принимает `Task` (void = авто-назад) и `Task<IEndpointResult>` вместо только `Task<InputResult>` (now accepts `Task` (void = auto-back) and `Task<IEndpointResult>` instead of only `Task<InputResult>`)
- `AdminRoadmapHandler` переведён на `MapAction<ClearRoadmapAction>` и `MapInput<SetRoadmapInput>` (migrated to typed actions)
- `AdminRoadmapScreen` переведён на `Button<ClearRoadmapAction>` вместо строкового callback ID (migrated to `Button<ClearRoadmapAction>` from string callback ID)
- `SetRoadmapInputScreen` переведён на `AwaitInput<SetRoadmapInput>()` вместо `ACTION_ID` константы (migrated to `AwaitInput<SetRoadmapInput>()` from `ACTION_ID` constant)
- Актуализированы `README.md`, `docs/API.md`, `docs/ARCHITECTURE.md` (updated documentation)
- Fallback перенесён в `Features/Fallback/FallbackEndpoints.cs` (fallback moved to `Features/Fallback/FallbackEndpoints.cs`)
- Roadmap: обработка разнесена по отдельным endpoint-классам — `GetRoadmapEndpoint`, `ClearRoadmapEndpoint`, `SetRoadmapInputEndpoint` (Roadmap split into separate endpoint classes)

### Удалено (Removed)

- `InputResult` — заменён на `IEndpointResult` + `BotResults` (replaced by `IEndpointResult` + `BotResults`)
- `ResultStrategy` — логика диспатча перенесена в `IEndpointResult.ExecuteAsync` (dispatch logic moved to `IEndpointResult.ExecuteAsync`)
- Легаси-команда `/clear_roadmap` из `AdminRoadmapHandler` (legacy `/clear_roadmap` command removed from `AdminRoadmapHandler`)
- Отдельный класс `NavigationHandler` в App — навигация `nav:*` встроена во фреймворк через `UseNavigation<T>()` (standalone `NavigationHandler` in App removed; `nav:*` navigation built into framework via `UseNavigation<T>()`)

## [0.2.0] — 2026-02-18

### Добавлено (Added)

- Проект `TelegramBotFlow.Core.Data` — data layer фреймворка (framework data layer)
    - `BotUser` — базовая сущность пользователя (base user entity, inheritable like ASP.NET Identity)
    - `BotDbContext<TUser>` — generic DbContext
    - `UserTrackingMiddleware<TUser>` — автоматическое отслеживание пользователей (automatic user tracking, generic + non-generic)
    - `AddBotCoreData()` / `AddBotCoreData<TUser, TContext>()` — DI extensions
    - EF Core миграция `InitUsers` (migration, таблица / table `users`)
- Модуль рассылок `TelegramBotFlow.Broadcasts` (Broadcast module)
    - Domain-модели (domain models): `Broadcast`, `BroadcastSequence`, `BroadcastSequenceStep`, `UserSequenceProgress`
    - EF Core + PostgreSQL для хранения данных рассылок (for broadcast data storage)
    - Quartz.NET для фонового выполнения задач (for background job execution, PostgreSQL persistence)
    - REST API для мониторинга и управления рассылками через Swagger UI (for monitoring & managing broadcasts via Swagger UI)
    - `BroadcastSender` — отправка через `copyMessage` с rate limiting (sending via `copyMessage` with rate limiting)
    - `SequenceProcessorJob` — обработка последовательных рассылок каждую минуту (processes sequential broadcasts every minute)
    - `BroadcastExecutionJob` — выполнение ручных рассылок (executes manual broadcasts)
    - `AdminBroadcastEndpoint` — создание рассылок через Telegram (broadcast creation via Telegram, IBotEndpoint)
- `MapAction` — новый метод роутинга для action-кнопок (new routing method for action buttons)
    - автоматически отвечает на callback (auto-answers callback, removes spinner)
    - если обработчик возвращает `ScreenView` — показывает его в nav-сообщении (if handler returns `ScreenView` — shows it in nav message)
- `ScreenView` — rich builder для экранов (rich screen builder)
    - `NavigateButton<TScreen>(text)` — кнопка навигации по типу экрана (type-based navigation button)
    - `BackButton()` / `CloseButton()` / `MenuButton()` — стандартные кнопки навигации (standard navigation buttons)
    - `UrlButton(text, url)` — кнопка-ссылка (URL link button)
    - Медиа-вложения (media attachments): `WithPhoto()`, `WithVideo()`, `WithAnimation()`, `WithDocument()`
- `Features/` — папка для `IBotEndpoint` и `IScreen` классов в App (folder for `IBotEndpoint` and `IScreen` classes in App)
    - `Features/Start/` — `StartHandler` (`/start`, `/help`)
    - `Features/Navigation/` — `NavigationHandler` — обработчик `nav:*` callback (handler for `nav:*` callbacks: back, close, menu, navigation by ID)
    - `Features/Roadmap/` — `RoadmapHandler` — action-кнопка `get_roadmap` с отображением roadmap (action button displaying roadmap)
    - `Features/Roadmap/` — `AdminRoadmapHandler`, `AdminRoadmapScreen`, `SetRoadmapInputScreen`, `ClearRoadmapAction`, `SetRoadmapInput`
    - `Features/MainMenu/` — `MainMenuScreen`
    - `Features/Profile/` — `ProfileScreen`
    - `Features/Settings/` — `SettingsScreen`
    - `Features/Help/` — `HelpScreen`
- `IUpdateResponder` / `UpdateResponder` — сервис отправки ответов пользователю (user response service)
- `IUserAccessPolicy` / `BotConfigurationUserAccessPolicy` — политика доступа администратора (admin access policy)
- `AccessPolicyMiddleware` — middleware для вычисления `UpdateContext.IsAdmin` (middleware for computing `UpdateContext.IsAdmin`)
- `BotRuntime` — класс запуска бота (bot startup class, Polling / Webhook)
- `HandlerDelegateFactory` — фабрика делегатов для DI-резолюции параметров обработчиков (delegate factory for handler DI parameter resolution)
- PostgreSQL в docker-compose для хранения данных (PostgreSQL in docker-compose for data storage)
- Документация (documentation): `API.md`, `ARCHITECTURE.md`, `CHANGELOG.md`, `INFRASTRUCTURE.md`, `CODE_STYLE.md`

### Изменено (Changed)

- `IScreen` — удалено свойство `Id` (removed `Id` property); идентификатор экрана теперь вычисляется автоматически по имени класса (screen ID now auto-computed from class name): суффикс `Screen` обрезается, результат переводится в `snake_case` (`MainMenuScreen` → `main_menu`, `SettingsLangScreen` → `settings_lang`)
- `ScreenRegistry` — убран `Activator.CreateInstance` для чтения `Id` (removed `Activator.CreateInstance`); добавлены статические методы `GetIdFromType(Type)` и `GetIdFor<TScreen>()`
- `AddScreens()` — устранён анти-паттерн `BuildServiceProvider()` (anti-pattern eliminated); экраны регистрируются через factory-singleton (screens registered via factory-singleton)
- `AddTelegramBotFlow()` — `ScreenRegistry` регистрируется через `TryAddSingleton`
- `ScreenManager.RenderScreenAsync` — метод стал `internal` (method made `internal`); публичный API навигации / public navigation API — только `NavigateToAsync`
- `UserSession.ClearScreen()` → `ClearCurrentScreen()` — уточнено название метода (method renamed for clarity)
- Пользователи (`BotUser`) вынесены из Broadcasts в Core.Data (users moved from Broadcasts to Core.Data); `BroadcastsDbContext` содержит только broadcast-специфичные таблицы (contains only broadcast-specific tables)
- Посты рассылок теперь создаются через Telegram `copyMessage` (broadcast posts now created via Telegram `copyMessage`): `Broadcast` и `BroadcastSequenceStep` хранят `FromChatId` + `MessageId` вместо `Content` (store `FromChatId` + `MessageId` instead of `Content`); удалены `POST /api/broadcasts` и `POST /api/sequences` (removed)
- `UpdateContext` стал data-only объектом (became data-only object): удалены convenience-методы ответа и service locator API (removed convenience response methods and service locator API); отправка вынесена в `IUpdateResponder` (sending extracted to `IUpdateResponder`); флаг `IsAdmin` вычисляется в `UseAccessPolicy()` (`IsAdmin` computed in `UseAccessPolicy()`)
- `BotApplication` middleware pipeline: `UseAccessPolicy()` добавлен как стандартный этап (added as standard stage)
- `AdminSequenceEndpoint`: Telegram-мастер создания последовательности временно отключён (Telegram sequence creation wizard temporarily disabled); `/sequence` возвращает инструкцию использовать REST API (returns instruction to use REST API)
- Redis session store перемещён в подпапку `Sessions/Redis/` (moved to `Sessions/Redis/` subfolder)
- `Handlers/` и `Screens/` в App переименованы в `Features/` (renamed to `Features/`); файлы организованы по фичам (files organized by feature, see [CODE_STYLE.md](CODE_STYLE.md))
- Принят стиль `SCREAMING_CASE` для констант и enum (adopted `SCREAMING_CASE` for constants and enums, see [CODE_STYLE.md](CODE_STYLE.md))

### Удалено (Removed)

- `FlowBuilder`, `FlowDefinition`, `FlowManager`, `FlowStep`, `Validators` — модуль Flow удалён (Flow module removed, replaced by `Conversations/` in the future)
- `ThrottlingMiddleware` / `ThrottlingOptions` — Rate limiting удалён из фреймворка (removed from the framework)
- `RedisSessionStore` из корня `Sessions/` (removed from `Sessions/` root, moved to `Sessions/Redis/`)

## [0.1.0] — 2025-02-01

### Добавлено (Added)

- Базовый фреймворк Telegram Bot Flow (Base Telegram Bot Flow framework)
    - Middleware pipeline (ErrorHandling, Logging, Throttling, Session, Flow)
    - Minimal API-стиль роутинга (Minimal API-style routing: `MapCommand`, `MapCallback`, `MapMessage`, `MapFlow`)
    - `IBotEndpoint` auto-discovery
    - `FlowBuilder` для пошаговых диалогов (for step-by-step dialogs)
    - `InlineKeyboard` / `ReplyKeyboard` UI-компоненты (UI components)
    - `ISessionStore` (InMemory / Redis)
    - Rate limiting (Sliding Window, per-user)
    - Polling / Webhook режимы (modes)
    - Serilog + Seq для логирования (for logging)
    - Docker Compose
