# Руководство по использованию Telegram Bot Flow

Это руководство описывает, как пользоваться проектом при разработке своего бота: логика меню, навигация, управление UI и какие методы вызывать. Предполагается, что вы впервые подключаетесь к проекту.

---

## 1. Общая картина

Бот строится вокруг **экранов** (screens) и **действий** (actions). Пользователь перемещается по экранам, нажимает кнопки (callback) или вводит текст; каждый тип взаимодействия маппится на обработчик через маршрутизацию.

- **Экран** — это «страница» диалога: текст + inline-кнопки. Реализуется через `IScreen` и возвращает `ScreenView`.
- **Навигация** — переходы между экранами с историей (стек). Есть кнопки «Назад», «Главное меню», переход по экрану.
- **Действие** — нажатие кнопки или ввод текста, привязанное к типу `IBotAction` и обрабатываемое в endpoint'ах через `MapAction` / `MapInput`.

Типичный поток:

1. Пользователь отправляет `/start` → открывается главное меню (экран).
2. На главном меню кнопки ведут на другие экраны или на действия (например «Получить Roadmap»).
3. На экране может быть ожидание ввода текста (`AwaitInput`) — следующее сообщение уйдёт в зарегистрированный input-обработчик.

---

## 2. Точка входа: Program.cs и BotApplication

В `Program.cs` вы создаёте приложение бота, подключаете middleware и регистрируете маршруты.

```csharp
BotApplicationBuilder builder = BotApplication.CreateBuilder(args);
builder.Services.AddBotCoreData(builder.Configuration);

BotApplication app = builder.Build();

// Порядок middleware важен
app.UseErrorHandling();
app.UseLogging();
app.UsePrivateChatOnly();
app.UseSession();        // сессия пользователя (навигация, PendingInput)
app.UseAccessPolicy();   // ctx.IsAdmin
app.UsePendingInput();   // маршрутизация текста в input-обработчики

app.SetMenu(menu => menu.Command("start", "Главное меню"));

// Обработка nav:back, nav:close, nav:menu и nav:{screenId}
app.UseNavigation<MainMenuScreen>();

// Регистрация всех IBotEndpoint из сборки (команды, actions, input, fallback)
app.MapBotEndpoints();

await app.RunAsync();
```

- **UseSession** — обязательно, если используете экраны и навигацию (стек, `CurrentScreen`, `PendingInputActionId`).
- **UsePendingInput** — обязательно, если есть экраны с `AwaitInput`; должен идти после `UseSession`.
- **UseNavigation&lt;TMenuScreen&gt;** — обрабатывает callback'и `nav:back`, `nav:close`, `nav:menu` и переходы по `screenId`. Вызывать **до** `MapBotEndpoints()`.

---

## 3. Экраны (Screens) и UI

### 3.1. Что такое экран

Экран — класс, реализующий `IScreen`:

```csharp
public interface IScreen
{
    Task<ScreenView> RenderAsync(UpdateContext ctx);
}
```

Один экран = одно «сообщение» с текстом и inline-клавиатурой.

**Идентификатор экрана (ID)** вычисляется по имени типа: суффикс `Screen` отбрасывается, остаток приводится к snake_case. Например: `MainMenuScreen` → `main_menu`, `SetRoadmapInputScreen` → `set_roadmap_input`. Классы без суффикса `Screen` (например `ProfilePage`) тоже регистрируются, но ID будет по полному имени: `profile_page`. Для предсказуемых коротких ID именуйте экраны с суффиксом `Screen`.

### 3.2. Построение представления (ScreenView)

`ScreenView` — fluent builder: текст, кнопки, медиа, ожидание ввода.

**Текст и кнопки:**

```csharp
public Task<ScreenView> RenderAsync(UpdateContext ctx)
{
    var view = new ScreenView("Добро пожаловать! Выберите раздел:")
        .NavigateButton<ProfileScreen>("Профиль")
        .NavigateButton<SettingsScreen>("Настройки")
        .Row()
        .Button<GetRoadmapAction>("🗺 Получить Roadmap");

    if (ctx.IsAdmin)
        view.Row().NavigateButton<AdminRoadmapScreen>("⚙️ Настройки Roadmap");

    return Task.FromResult(view);
}
```

Основные методы `ScreenView`:

| Метод                                         | Назначение                                                                               |
| --------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `new ScreenView(text)`                        | Создать экран с текстом.                                                                 |
| `.NavigateButton<TScreen>(text)`              | Кнопка перехода на экран (callback `nav:{screenId}`).                                    |
| `.Button<TAction>(text)`                      | Кнопка действия (callback = имя типа `TAction`).                                         |
| `.Button<TAction, TPayload>(text, payload)`   | Кнопка действия с payload (короткий JSON или shortId).                                   |
| `.Button(text, callbackData)`                 | Произвольная callback-кнопка.                                                            |
| `.UrlButton(text, url)`                       | Кнопка-ссылка.                                                                           |
| `.Row()`                                      | Новая строка кнопок.                                                                     |
| `.BackButton(text)`                           | «← Назад» — возврат по стеку (`nav:back`).                                               |
| `.CloseButton(text)`                          | «← Назад» без pop стека — перерисовка текущего экрана (`nav:close`).                     |
| `.MenuButton(text)`                           | «☰ Главное меню» — сброс навигации и переход в главное меню (`nav:menu`).               |
| `.AwaitInput<TAction>()`                      | После показа экрана следующий текст пользователя уйдёт в input-обработчик для `TAction`. |
| `.WithPhoto(url)` / `.WithVideo(file)` и т.д. | Прикрепить медиа к сообщению.                                                            |

Константы навигационных callback'ов: `NavCallbacks.Back`, `NavCallbacks.Close`, `NavCallbacks.Menu`.

### 3.3. Когда что использовать

- **NavigateButton** — переход на другой экран (добавляется в стек навигации).
- **Button&lt;TAction&gt;** — действие без данных (например «Получить Roadmap», «Удалить»).
- **Button&lt;TAction, TPayload&gt;** — действие с данными (например id элемента для удаления).
- **BackButton** — на экранах внутри потока, когда есть «родитель» в стеке.
- **MenuButton** — на «листовых» экранах (результат действия, информация), чтобы вернуться в корень без истории.
- **CloseButton** — когда нужно только обновить текущий экран, не уходя назад по стеку.

### 3.4. Параметры перехода (аргументы навигации)

Чтобы передать данные на экран при переходе (например ID сущности для экрана просмотра), задайте аргументы в сессии **до** возврата результата навигации, а в целевом экране прочитайте их в `RenderAsync`.

В обработчике перед переходом:

```csharp
ctx.Session?.SetNavigationArg("UserId", userId);
return BotResults.NavigateTo<ProfileScreen>();
```

Типизированная перегрузка (сериализация в JSON):

```csharp
ctx.Session?.SetNavigationArg("ItemId", itemId);
return BotResults.NavigateTo<ItemDetailScreen>();
```

В целевом экране в `RenderAsync`:

```csharp
long? userId = ctx.Session?.GetNavigationArg<long>("UserId");
string? itemId = ctx.Session?.GetNavigationArg<string>("ItemId");
```

Аргументы автоматически очищаются после отрисовки экрана, к которому перешли. Повторный вызов `GetNavigationArg` на том же экране вернёт то же значение до конца текущего рендера.

---

## 4. Навигация

Навигация хранится в сессии: `CurrentScreen`, стек `NavigationStack`, «якорное» сообщение `NavMessageId`. Управлять переходом можно из кода через `IScreenNavigator` или возвращая результат из обработчика.

### 4.1. Результаты обработчиков (BotResults)

Обработчики команд, callback и input возвращают `IEndpointResult`. Фабрика — `BotResults`:

| Метод                                      | Эффект                                                                                                              |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| `BotResults.NavigateTo<TScreen>()`         | Переход на экран (push в стек).                                                                                     |
| `BotResults.NavigateTo(screenId)`          | То же по строковому ID.                                                                                             |
| `BotResults.NavigateToRoot<TScreen>()`     | Переход на экран с очисткой всей истории (стек сбрасывается). После визарда или критичного действия.               |
| `BotResults.NavigateToRoot(screenId)`      | То же по строковому ID.                                                                                             |
| `BotResults.Back(notification?)`           | Возврат на предыдущий экран (pop), опционально ответ в callback. Для обработчиков кнопок/input.                    |
| `BotResults.Refresh(notification?)`        | Перерисовать текущий экран без изменения стека. Если текущий экран не задан — без эффекта (no-op).                  |
| `BotResults.ShowView(ScreenView)`          | Показать разовое представление (без смены экрана). При необходимости автоматически добавится кнопка «Главное меню». |
| `BotResults.Empty()`                       | Ничего не менять (например, сообщение уже отправлено вручную).                                                      |
| `BotResults.Stay(notification?, deleteMessage?)` | Остаться в режиме ввода. По умолчанию удаляет сообщение пользователя; `deleteMessage: false` — только ответить на callback. |

Примеры:

```csharp
// Команда /start — очистить сессию и открыть главное меню
app.MapCommand("/start", async (UpdateContext ctx) =>
{
    ctx.Session?.Clear();
    return BotResults.NavigateTo<MainMenuScreen>();
});

// Кнопка «Получить Roadmap» — показать контент или fallback
return BotResults.ShowView(new ScreenView(ROADMAP_FALLBACK_TEXT).MenuButton());

// После сохранения ввода — назад и уведомление
return BotResults.Back("✅ Roadmap успешно сохранён");

// После завершения визарда — перейти в корень (очистить историю)
return BotResults.NavigateToRoot<MainMenuScreen>();

// Остаться в вводе, но не удалять сообщение пользователя (только toast)
return BotResults.Stay("Попробуйте ещё раз", deleteMessage: false);
```

**Когда что использовать:**

- **Back(notification)** — в обработчиках callback-кнопок и input: возврат на предыдущий экран и (при наличии) ответ на callback (убирает «часики»). Из кода без callback можно тоже возвращать `Back()`: ответ на callback будет пропущен.
- **GoBackAsync** (через `IScreenNavigator`) — программный возврат без ответа на callback, когда вы сами вызываете навигацию в коде, а не возвращаете результат из handler'а.
- **Refresh()** — имеет эффект только если в сессии задан текущий экран (`CurrentScreen`). Иначе выполняется без изменений.
- **Stay()** — по умолчанию удаляет сообщение пользователя (удобно скрыть неверный ввод). Чтобы только показать уведомление и оставить сообщение, используйте `Stay(notification, deleteMessage: false)`.

### 4.2. Навигатор в коде (IScreenNavigator)

Если нужно перейти или показать view из сервиса/обработчика с произвольной логикой:

```csharp
// Переход на экран
await _navigator.NavigateToAsync(ctx, typeof(ProfileScreen));
await _navigator.NavigateToAsync<ProfileScreen>(ctx);
await _navigator.NavigateToAsync(ctx, "profile");

// Назад по стеку (без ответа на callback)
await _navigator.GoBackAsync(ctx);

// Назад и ответ на callback (убрать «часики» + опционально текст)
await _navigator.NavigateBackAsync(ctx, "✅ Сохранено");

// Перерисовать текущий экран
await _navigator.RefreshScreenAsync(ctx);

// Показать разовое представление без смены экрана
await _navigator.ShowViewAsync(ctx, new ScreenView("Временное сообщение").MenuButton());
```

В обработчиках маршрутов обычно достаточно возвращать `BotResults.*`, навигацию выполнит фреймворк.

---

## 5. Регистрация маршрутов (Endpoint'ы)

Вся регистрация идёт через `BotApplication` и классы, реализующие `IBotEndpoint`. `MapBotEndpoints()` вызывает `MapEndpoint(app)` у каждого зарегистрированного `IBotEndpoint`.

### 5.1. Команды

```csharp
app.MapCommand("/start", async (UpdateContext ctx) =>
{
    ctx.Session?.Clear();
    return BotResults.NavigateTo<MainMenuScreen>();
});

app.MapCommand("/help", () => Task.FromResult(BotResults.NavigateTo<HelpScreen>()));
```

Команда может быть с или без `/`. В контексте доступен `CommandArgument` (часть после пробела).

### 5.2. Кнопки-действия (callback)

Типизированное действие без payload:

```csharp
public struct GetRoadmapAction : IBotAction { }

app.MapAction<GetRoadmapAction>(Handle);

private static async Task<IEndpointResult> Handle(
    UpdateContext ctx, BotDbContext db, IUpdateResponder responder)
{
    // ...
    return BotResults.ShowView(new ScreenView(text).MenuButton());
}
```

С payload (например id сущности):

```csharp
public struct DeleteItemAction : IBotAction { }

app.MapAction<DeleteItemAction, Guid>(HandleWithPayload);

private static async Task<IEndpointResult> HandleWithPayload(
    UpdateContext ctx, Guid itemId, BotDbContext db)
{
    // itemId приходит из кнопки Button<DeleteItemAction, Guid>("Удалить", itemId)
    // ...
    return BotResults.Back("Удалено");
}
```

Callback по кнопке автоматически закрывается (AnswerCallback), затем вызывается ваш handler.

### 5.3. Ожидание ввода текста (Input)

Экран объявляет ожидание ввода:

```csharp
new ScreenView("Отправьте сообщение для Roadmap...")
    .BackButton()
    .AwaitInput<SetRoadmapInput>();
```

Регистрация обработчика этого ввода:

```csharp
public struct SetRoadmapInput : IBotAction { }

app.MapInput<SetRoadmapInput>(Handle);

private static async Task<IEndpointResult> Handle(
    UpdateContext ctx, BotDbContext db, ITelegramBotClient bot, ...)
{
    // ctx.MessageId, ctx.ChatId — отправленное пользователем сообщение
    // ...
    return BotResults.Back("✅ Сохранено");
}
```

Когда пользователь на экране с `AwaitInput<SetRoadmapInput>()` отправит любое сообщение (текст/фото и т.д.), его перехватит `PendingInputMiddleware` и вызовет зарегистрированный handler для `SetRoadmapInput`. После перехода на другой экран или смены контекста `PendingInputActionId` сбрасывается.

### 5.4. Fallback

Если ни один маршрут не подошёл (в т.ч. не команда и не pending input):

```csharp
app.MapFallback(async (UpdateContext ctx, IUpdateResponder responder) =>
{
    await responder.ReplyAsync(ctx, "Не понимаю. Нажмите кнопку в меню или /start");
    return BotResults.Empty();
});
```

---

## 6. Сессия пользователя (UserSession)

Доступна в `UpdateContext` как `ctx.Session` (может быть null, если session middleware не подключён).

- **Навигация:** `CurrentScreen`, `NavigationStack`, `NavMessageId`, `CurrentMediaType`. Менять стек только через результат навигации или через `Session.ResetNavigation()` / `Session.Clear()` в коде.
- **Ожидание ввода:** `PendingInputActionId` — выставляется экраном через `AwaitInput`, сбрасывается при переходе на другой экран или при явной очистке.
- **Данные:** `Set(key, value)`, `GetString(key)`, `GetInt(key)`, `GetBool(key)`, `Has(key)`, `Remove(key)`.
- **Состояние (typed):** `SetState<T>(state)`, `GetState<T>()`, `RemoveState<T>()`.
- **Аргументы перехода:** `SetNavigationArg(key, value)` / `SetNavigationArg<T>(key, value)` перед переходом; в целевом экране — `GetNavigationArg(key)` / `GetNavigationArg<T>(key)`. Очищаются автоматически после отрисовки экрана. См. раздел 3.4.
- **Payload кнопок:** при использовании `Button<TAction, TPayload>(text, payload)` большие payload сохраняются в сессии по shortId; в handler с `MapAction<TAction, TPayload>` параметр `TPayload` приходит после десериализации из сессии или из встроенного в callback JSON.

Полезные методы сессии:

- `Clear()` — полная очистка (данные + навигация + pending input). Уместно для `/start`.
- `ResetNavigation()` — сброс стека и текущего экрана, сохранение данных и якорного сообщения. Используется при переходе в главное меню (`nav:menu`).

---

## 7. Меню команд (MenuBuilder)

Отображается в Telegram рядом с полем ввода:

```csharp
app.SetMenu(menu => menu
    .Command("start", "Главное меню")
    .AdminCommand("admin", "Панель администратора"));
```

`Command` — для всех, `AdminCommand` — дополнительно для пользователей из списка админов (настраивается в конфигурации доступа). Вызов `SetMenu` обязателен до `RunAsync()`, иначе меню не применится.

---

## 8. Контекст обновления (UpdateContext)

В обработчиках доступен `UpdateContext ctx`:

- `ctx.Update` — исходный Update.
- `ctx.ChatId`, `ctx.UserId`, `ctx.MessageId` — чат, пользователь, сообщение.
- `ctx.Session` — сессия пользователя.
- `ctx.IsAdmin` — флаг администратора (после AccessPolicy).
- `ctx.CallbackData` — данные callback-кнопки.
- `ctx.MessageText` — текст сообщения.
- `ctx.CommandArgument` — аргумент команды (после пробела).
- `ctx.UpdateType` — тип update.
- `ctx.RequestServices` — scope DI для резолва сервисов.
- `ctx.CancellationToken` — отмена.

---

## 9. Порядок регистрации и pipeline

Рекомендуемый порядок в `Program.cs`:

1. Middleware: ErrorHandling → Logging → PrivateChatOnly → Session → AccessPolicy → … → PendingInput.
2. `SetMenu(...)`.
3. `UseNavigation<TMainMenuScreen>()`.
4. `MapBotEndpoints()` (внутри — команды, actions, input, fallback из всех `IBotEndpoint`).

Маршруты проверяются в порядке добавления. Навигационные callback'и `nav:*` обрабатываются через `UseNavigation`; остальные callback'и и команды — через зарегистрированные в endpoint'ах маршруты. Input обрабатывается только если установлен `PendingInputActionId` и сработал `PendingInputMiddleware`.

---

## 10. Краткий чеклист: добавить новую фичу

1. **Новый экран:** класс `XxxScreen : IScreen` (суффикс `Screen` даёт короткий ID по конвенции), метод `RenderAsync` возвращает `ScreenView`. Экраны из сборки регистрируются автоматически.
2. **Кнопка перехода на экран:** `.NavigateButton<XxxScreen>("Текст")`. Если нужны параметры — перед переходом `ctx.Session?.SetNavigationArg(key, value)`, в целевом экране `ctx.Session?.GetNavigationArg<T>(key)` (раздел 3.4).
3. **Кнопка-действие:** объявить `struct XxxAction : IBotAction`, в endpoint'е `app.MapAction<XxxAction>(Handler)`, в экране — `.Button<XxxAction>("Текст")`.
4. **Действие с payload:** `app.MapAction<XxxAction, TPayload>(Handler)`, в экране — `.Button<XxxAction, TPayload>("Текст", payload)`.
5. **Ожидание ввода:** на экране `.AwaitInput<XxxInput>()`, объявить `struct XxxInput : IBotAction`, в endpoint'е `app.MapInput<XxxInput>(Handler)`. В handler'е — `BotResults.Back("Готово")` или `BotResults.NavigateToRoot<MainMenuScreen>()`.
6. **Команда:** в любом `IBotEndpoint` вызвать `app.MapCommand("/cmd", handler)`.
7. **Переход в корень (очистить историю):** `BotResults.NavigateToRoot<TScreen>()` или `BotResults.NavigateToRoot(screenId)`.
8. **Fallback:** один раз зарегистрировать `app.MapFallback(handler)` (например в `FallbackEndpoints`).

Этого достаточно, чтобы уверенно пользоваться ботом: строить меню, навигацию и UI через экраны и действия и при необходимости расширять проект своими экранами и endpoint'ами.

