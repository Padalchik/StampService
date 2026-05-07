# Справочник API (API Reference)

## BotApplication

Центральный класс фреймворка. Создаёт и запускает bot runtime.

The central framework class. Creates and runs the bot runtime.

### Создание и запуск (Creation & Startup)

```csharp
var builder = BotApplication.CreateBuilder(args);

builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());

var app = builder.Build();

app.UseErrorHandling();
app.UseLogging();
app.UseSession();
app.UseAccessPolicy();
app.UsePendingInput();

app.SetMenu(menu => menu.Command("start", "Главное меню"));
app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();
await app.RunAsync();
```

### Middleware

| Метод / Method       | Описание / Description                                                                                                                                                                    |
| -------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `UseErrorHandling()` | Ловит исключения, логирует и отправляет error message / Catches exceptions, logs and sends error message                                                                                  |
| `UseLogging()`       | Логирует вход/выход и время обработки / Logs entry/exit and processing time                                                                                                               |
| `UseSession()`       | Загружает и сохраняет `UserSession` / Loads and saves `UserSession`                                                                                                                       |
| `UseAccessPolicy()`  | Заполняет `UpdateContext.IsAdmin` / Populates `UpdateContext.IsAdmin`                                                                                                                     |
| `UsePendingInput()`  | Маршрутизирует следующий текст в `MapInput`-обработчик / Routes next text to `MapInput` handler                                                                                           |
| `UseNavigation<T>()` | Регистрирует обработчик callback `nav:*` для навигации по экранам; `T` — экран главного меню / Registers handler for `nav:*` callbacks for screen navigation; `T` is the main menu screen |
| `Use<TMiddleware>()` | Подключает кастомный `IUpdateMiddleware` / Plugs in custom `IUpdateMiddleware`                                                                                                            |

### Routing (Маршрутизация)

| Метод / Method                      | Что обрабатывает / Handles                                      | Пример / Example                                                   |
| ----------------------------------- | --------------------------------------------------------------- | ------------------------------------------------------------------ |
| `MapCommand(command, handler)`      | Команды / Commands                                              | `app.MapCommand("/start", handler)`                                |
| `MapCallback(pattern, handler)`     | Callback-кнопки / Callback buttons                              | `app.MapCallback("profile", handler)`                              |
| `MapAction(callbackId, handler)`    | Action-кнопки / Action buttons (авто-ответ + ScreenView)        | `app.MapAction("get_roadmap", handler)`                            |
| `MapCallbackGroup(prefix, handler)` | Callback с префиксом / Callback by prefix                       | `app.MapCallbackGroup("broadcast", handler)`                       |
| `MapInput(actionId, handler)`       | Ожидаемый пользовательский ввод / Expected user input           | `app.MapInput("roadmap_set_message", handler)`                     |
| `MapMessage(predicate, handler)`    | Сообщения по предикату / Messages by predicate                  | `app.MapMessage(ctx => ctx.MessageText == "Да", handler)`          |
| `MapUpdate(predicate, handler)`     | Любой update по предикату / Any update by predicate             | `app.MapUpdate(ctx => ctx.Update.Message?.Photo != null, handler)` |
| `MapFallback(handler)`              | Fallback, если route не найден / Fallback when no route matched | `app.MapFallback(handler)`                                         |

#### MapAction

`MapAction` — специализированный `MapCallback` для кнопок-действий:

`MapAction` is a specialized `MapCallback` for action buttons:

- автоматически отвечает на callback (убирает часики с кнопки) / automatically answers the callback (removes the spinner)
- если обработчик возвращает `ScreenView`, показывает его в nav-сообщении с кнопкой "← Назад" / if the handler returns a `ScreenView`, displays it in a nav message with a "← Back" button

```csharp
app.MapAction("get_roadmap", () =>
    Task.FromResult(new ScreenView("Текст ответа").MenuButton()));
```

Также поддерживает типизированные параметры (Payloads), сериализуемые в сессию для обхода ограничения Telegram в 64 байта:

```csharp
// Регистрирует маршрут, который автоматически десериализует TPayload
app.MapAction<DeleteUserAction, DeletePayload>(async (DeletePayload payload) =>
{
    // payload типизирован
});
```

### SetMenu (Меню бота)

```csharp
app.SetMenu(menu => menu
    .Command("start", "Главное меню"));
```

Устанавливает список команд бота, отображаемый при нажатии `/` в Telegram.

Sets the bot command list displayed when pressing `/` in Telegram.

### UseNavigation\<TMenuScreen\> (Навигация по экранам)

Регистрирует встроенный обработчик callback-ов с префиксом `nav:*` (кнопки навигации из `ScreenView`: переход к экрану, «Назад», «Главное меню»). Параметр типа `TMenuScreen` — экран главного меню, на который ведёт кнопка «Главное меню» (`MenuButton`).

Registers the built-in handler for callbacks with prefix `nav:*` (navigation buttons from `ScreenView`: navigate to screen, Back, Main menu). The type parameter `TMenuScreen` is the main menu screen shown when the user presses the «Main menu» button (`MenuButton`).

```csharp
app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();
```

Рекомендуется вызывать после `SetMenu` и перед `MapBotEndpoints`. Отдельный класс-обработчик для `nav:*` в приложении не нужен.

Recommended order: after `SetMenu`, before `MapBotEndpoints`. No separate handler class for `nav:*` in the app is required.

### Внедрение зависимостей в обработчик (Handler Dependency Injection)

Параметры обработчика резолвятся автоматически:

Handler parameters are resolved automatically:

- `UpdateContext` передаётся всегда / always provided
- `CancellationToken` берётся из контекста / taken from context
- сервисы (например `IUpdateResponder`, `IScreenNavigator`, DbContext) берутся из DI scope update-а / services (e.g. `IUpdateResponder`, `IScreenNavigator`, DbContext) are resolved from the update DI scope
- для `MapCallbackGroup` первый параметр `string` получает action-часть callback / for `MapCallbackGroup` the first `string` parameter receives the action part of the callback

Для навигации по экранам (`nav:*`) рекомендуется использовать `UseNavigation<TMenuScreen>()` (см. выше). Ручная регистрация через `MapCallbackGroup("nav", ...)` возможна, если нужна кастомная логика:

For screen navigation (`nav:*`), use `UseNavigation<TMenuScreen>()` (see above). Manual registration via `MapCallbackGroup("nav", ...)` is possible if custom logic is needed:

```csharp
app.MapCallbackGroup("nav", async (
    UpdateContext ctx,
    string action,
    IUpdateResponder responder,
    IScreenNavigator navigator) =>
{
    await responder.AnswerCallbackAsync(ctx);
    await navigator.NavigateToAsync(ctx, action);
});
```

## UpdateContext (Контекст обновления)

Контекст обновления — только данные update-а и runtime-состояние.

Update context — only update data and runtime state.

### Свойства (Properties)

| Свойство / Property | Тип / Type          | Описание / Description                                                  |
| ------------------- | ------------------- | ----------------------------------------------------------------------- |
| `Update`            | `Update`            | Исходный Telegram update / Original Telegram update                     |
| `CancellationToken` | `CancellationToken` | Токен отмены / Cancellation token                                       |
| `Session`           | `UserSession?`      | Текущая сессия / Current session (заполняется / filled by `UseSession`) |
| `IsAdmin`           | `bool`              | Флаг доступа / Access flag (заполняется / filled by `UseAccessPolicy`)  |
| `ChatId`            | `long`              | Chat ID                                                                 |
| `UserId`            | `long`              | User ID                                                                 |
| `MessageId`         | `int?`              | ID сообщения / Message ID                                               |
| `CallbackData`      | `string?`           | Callback data                                                           |
| `MessageText`       | `string?`           | Текст сообщения / Message text                                          |
| `CommandArgument`   | `string?`           | Аргумент команды после пробела / Command argument after space           |
| `UpdateType`        | `UpdateType`        | Тип update / Update type                                                |
| `Screen`            | `string?`           | Текущий экран из сессии / Current screen from session                   |

`UpdateContext` больше не содержит методов отправки сообщений и service locator API.

`UpdateContext` no longer contains message-sending methods or service locator API.

## IUpdateResponder (Сервис отправки ответов)

Сервис отправки ответов пользователю (вместо методов в `UpdateContext`).

Service for sending responses to the user (replaces methods previously in `UpdateContext`).

| Метод / Method                                                        | Описание / Description                                     |
| --------------------------------------------------------------------- | ---------------------------------------------------------- |
| `ReplyAsync(context, text, replyMarkup?, parseMode)`                  | Отправить сообщение / Send a message                       |
| `EditMessageAsync(context, text, replyMarkup?, parseMode)`            | Отредактировать текущее message / Edit the current message |
| `EditMessageAsync(context, messageId, text, replyMarkup?, parseMode)` | Отредактировать message по ID / Edit a message by ID       |
| `DeleteMessageAsync(context)`                                         | Удалить текущее message / Delete the current message       |
| `DeleteMessageAsync(context, messageId)`                              | Удалить message по ID / Delete a message by ID             |
| `AnswerCallbackAsync(context, text?, showAlert)`                      | Ответить на callback / Answer a callback                   |

## IUserAccessPolicy (Политика доступа)

Политика доступа администратора:

Admin access policy:

```csharp
public interface IUserAccessPolicy
{
    bool IsAdmin(UpdateContext context);
}
```

Стандартная реализация: `BotConfigurationUserAccessPolicy`, использует `Bot:AdminUserIds`.

Default implementation: `BotConfigurationUserAccessPolicy`, uses `Bot:AdminUserIds`.

## IBotEndpoint (Auto-Discovery)

Интерфейс auto-discovery для endpoint-классов:

Auto-discovery interface for endpoint classes:

```csharp
public interface IBotEndpoint
{
    void MapEndpoint(BotApplication app);
}
```

Регистрация / Registration:

```csharp
builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());
app.MapBotEndpoints();
```

## IUpdateMiddleware (Контракт middleware)

Контракт middleware pipeline:

Middleware pipeline contract:

```csharp
public interface IUpdateMiddleware
{
    Task InvokeAsync(UpdateContext context, UpdateDelegate next);
}
```

Подключение / Usage:

```csharp
app.Use<MyMiddleware>();
```

## Screens API (API экранов)

`IScreen`:

```csharp
public interface IScreen
{
    Task<ScreenView> RenderAsync(UpdateContext ctx);
}
```

`IScreenNavigator`:

```csharp
Task NavigateToAsync(UpdateContext context, string screenId);
Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen;
Task GoBackAsync(UpdateContext context);
Task RefreshScreenAsync(UpdateContext context);
```

Идентификаторы экранов вычисляются по конвенции:

Screen identifiers are computed by convention:

- `MainMenuScreen` → `main_menu`
- `ProfileScreen` → `profile`
- `SettingsLangScreen` → `settings_lang`

### ScreenView (Представление экрана)

`ScreenView` — описание содержимого экрана: текст, медиа, кнопки.

`ScreenView` describes screen content: text, media, buttons.

#### Конструктор (Constructor)

```csharp
new ScreenView("Текст экрана")
```

#### Кнопки навигации (Navigation Buttons)

| Метод / Method                             | Описание / Description                                                                    |
| ------------------------------------------ | ----------------------------------------------------------------------------------------- |
| `NavigateButton<TScreen>(text)`            | Кнопка перехода к экрану / Navigate to screen button (`nav:{screenId}`)                   |
| `Button(text, callbackData)`               | Произвольная callback-кнопка / Arbitrary callback button                                  |
| `Button<TAction>(text)`                    | Типизированная кнопка действия / Typed action button                                      |
| `Button<TAction, TPayload>(text, payload)` | Типизированная кнопка с объектом payload / Typed action button with payload object        |
| `UrlButton(text, url)`                     | Кнопка-ссылка / URL link button                                                           |
| `Row()`                                    | Начать новую строку кнопок / Start a new button row                                       |
| `BackButton(text?)`                        | Кнопка "← Назад" / "← Back" button (pop навигационного стека / pops nav stack)            |
| `CloseButton(text?)`                       | Кнопка "← Назад" без изменения стека / "← Back" without stack change (for action results) |
| `MenuButton(text?)`                        | Кнопка "☰ Главное меню" / "☰ Main Menu" (полный сброс истории / full history reset)     |

#### Медиа (Media)

| Метод / Method                            | Описание / Description         |
| ----------------------------------------- | ------------------------------ |
| `WithPhoto(url)` / `WithPhoto(InputFile)` | Фото / Photo                   |
| `WithVideo(InputFile)`                    | Видео / Video                  |
| `WithAnimation(InputFile)`                | GIF / анимация / GIF/animation |
| `WithDocument(InputFile)`                 | Документ / Document            |

```csharp
new ScreenView("Описание")
    .WithPhoto(InputFile.FromUri("https://example.com/img.jpg"))
    .NavigateButton<ProfileScreen>("Перейти в профиль")
    .Row()
    .BackButton();
```

## UserSession (Сессия пользователя)

`UserSession` — key-value + навигационный state.

`UserSession` — key-value store + navigation state.

### Основные методы (Core Methods)

| Метод / Method            | Описание / Description                                         |
| ------------------------- | -------------------------------------------------------------- |
| `Set/GetString`           | Хранение строковых значений / String value storage             |
| `GetInt/GetLong/GetBool`  | Typed чтение / Typed reading                                   |
| `GetState<T>/SetState<T>` | Typed state через JSON / Typed state via JSON                  |
| `Has/Remove`              | Проверка/удаление ключа / Check/remove key                     |
| `PushScreen/PopScreen`    | Навигационный стек экранов / Navigation screen stack           |
| `ClearCurrentScreen`      | Очистка только текущего экрана / Clear only the current screen |
| `Clear`                   | Полный сброс сессии / Full session reset                       |

