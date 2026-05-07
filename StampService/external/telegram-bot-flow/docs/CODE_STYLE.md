# Стиль кода / Code Style

## Именование констант и enum / Constant & Enum Naming

В этом проекте для **констант** и **значений enum** используется стиль `SCREAMING_CASE` (верхний регистр с подчёркиванием).

In this project, **constants** and **enum values** use `SCREAMING_CASE` (uppercase with underscores).

```csharp
// Константы / Constants
public const string SECTION_NAME = "Bot";
public const int SINGLETON_ID = 1;
public const int BATCH_SIZE = 25;
public const int MAX_NAVIGATION_DEPTH = 20;

// Enum
public enum BotMode
{
    POLLING,
    WEBHOOK
}
```

> Это сознательное отступление от стандартного .NET PascalCase для констант и enum.
> Причина: визуальная отличимость констант от обычных свойств/методов.

> This is a deliberate deviation from the standard .NET PascalCase for constants and enums.
> Reason: visual distinction of constants from regular properties/methods.

## Прочие правила / Other Rules

| Правило / Rule                                                | Стиль / Style    |
| ------------------------------------------------------------- | ---------------- |
| Классы, методы, свойства / Classes, methods, properties       | `PascalCase`     |
| Локальные переменные, параметры / Local variables, parameters | `camelCase`      |
| Приватные поля / Private fields                               | `_camelCase`     |
| Константы / Constants                                         | `SCREAMING_CASE` |
| Enum значения / Enum values                                   | `SCREAMING_CASE` |
| Интерфейсы / Interfaces                                       | `IPascalCase`    |

## XML-документация / XML Documentation

- Весь код документируется на **русском языке** с `/// <summary>` тегами.
- All code is documented in **Russian** using `/// <summary>` tags.

## Структура App / App Structure

Фичи организованы в `Features/{FeatureName}/` — хэндлер, экран и action-маркеры рядом.

Features are organized in `Features/{FeatureName}/` — handler, screen, and action markers together.

```
Features/
  MainMenu/
    MainMenuScreen.cs
  Profile/
    ProfileScreen.cs
  Roadmap/
    GetRoadmapAction.cs      ← GetRoadmapAction + GetRoadmapEndpoint
    ClearRoadmapAction.cs    ← ClearRoadmapAction + ClearRoadmapEndpoint
    SetRoadmapInput.cs       ← SetRoadmapInput + SetRoadmapInputEndpoint
    AdminRoadmapScreen.cs
    SetRoadmapInputScreen.cs
  Fallback/
    FallbackEndpoints.cs
```

Допустимо и объединение всех Map\* одной фичи в один класс (например `RoadmapEndpoints.cs`), см. пример ниже.

A single `*Endpoints.cs` per feature (e.g. `RoadmapEndpoints.cs`) is also valid; see the example below.

## Routing conventions

### Один или несколько Endpoints-классов на фичу

Все `MapAction`, `MapInput`, `MapCommand` для фичи можно регистрировать в одном `*Endpoints.cs` либо разнести по нескольким endpoint-классам в одной папке (как в текущей реализации Roadmap: `GetRoadmapEndpoint`, `ClearRoadmapEndpoint`, `SetRoadmapInputEndpoint`).

All `MapAction`, `MapInput`, `MapCommand` for a feature may live in one `*Endpoints.cs` or be split across several endpoint classes in the same folder (as in the current Roadmap: `GetRoadmapEndpoint`, `ClearRoadmapEndpoint`, `SetRoadmapInputEndpoint`).

```csharp
// ✅ Один файл на фичу
public sealed class RoadmapEndpoints : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapAction<GetRoadmapAction>(...);
        app.MapAction<ClearRoadmapAction>(...);
        app.MapInput<SetRoadmapInput>(...);
    }
}
```

### IBotAction struct в файле Endpoints-класса

Маркеры действий объявляются в том же файле, где используется соответствующий Map\* (в одном общем файле фичи или рядом с endpoint-классом):

```csharp
// В начале файла RoadmapEndpoints.cs (если один класс на фичу)
public struct GetRoadmapAction : IBotAction;
public struct ClearRoadmapAction : IBotAction;
public struct SetRoadmapInput : IBotAction;

public sealed class RoadmapEndpoints : IBotEndpoint { ... }
```

При разбиении на несколько endpoint-классов каждый маркер — в начале своего файла (например `GetRoadmapAction` в `GetRoadmapAction.cs` вместе с `GetRoadmapEndpoint`).

When using multiple endpoint classes, each action marker stays at the top of its file (e.g. `GetRoadmapAction` in `GetRoadmapAction.cs` with `GetRoadmapEndpoint`).

### Все Map\* хэндлеры обязаны возвращать Task\<IEndpointResult\>

`void` и `Task` (без результата) не поддерживаются — фреймворк бросает `InvalidOperationException` при регистрации:

```csharp
// ✅ Правильно
app.MapCommand("/start", async (UpdateContext ctx) =>
{
    ctx.Session?.Clear();
    return BotResults.NavigateTo<MainMenuScreen>();
});

// ✅ Правильно — side-effect-only хэндлер возвращает Empty
app.MapFallback(async (UpdateContext ctx, IUpdateResponder responder) =>
{
    await responder.ReplyAsync(ctx, "Не понимаю.");
    return BotResults.Empty();
});

// ❌ Запрещено — void возврат
app.MapCommand("/start", async (UpdateContext ctx) =>
{
    await navigator.NavigateToAsync<MainMenuScreen>(ctx);
});
```

### Типизированные кнопки вместо magic strings

Используй `Button<TAction>` и `NavigateButton<TScreen>` вместо строковых callback-id:

```csharp
// ✅ Типизированная кнопка
view.Button<GetRoadmapAction>("🗺 Получить Roadmap");

// ❌ Magic string
view.Button("🗺 Получить Roadmap", "get_roadmap");
```

Если нужно вручную построить клавиатуру с навигационным callback (например, при `ReplaceAnchorWithCopyAsync`), используй константы `NavCallbacks` вместо raw строк:

```csharp
// ✅ Константа
InlineKeyboard.SingleButton("☰ Главное меню", NavCallbacks.MENU);

// ❌ Magic string
InlineKeyboard.SingleButton("☰ Главное меню", "nav:menu");
```

### Навигация встроена во фреймворк

Вызывай `app.UseNavigation<TMenuScreen>()` в `Program.cs` — `nav:*` callback-ы обрабатываются Core автоматически:

```csharp
app.UseNavigation<MainMenuScreen>();
app.MapBotEndpoints();
```

Отдельный `NavigationHandler` в App не нужен.

### SaveChangesAsync в App-слое

В App-слое (в хэндлерах `IBotEndpoint`) допустимо вызывать `db.SaveChangesAsync()` напрямую — транзакция ограничена обработкой одного Telegram Update. `ITransactionManager` из education-platform не используется: у бота нет outbox и событий домена, поэтому дополнительная абстракция избыточна.

```csharp
// ✅ В Bot App — допустимо
await db.SaveChangesAsync(ctx.CancellationToken);
```

В `TelegramBotFlow.Core` и `TelegramBotFlow.Broadcasts` этот подход также применяется: фреймворк не зависит от SachkovTech DDD-инфраструктуры.

