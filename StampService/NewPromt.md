# Operational Context: StampService

## 1. Цель проекта

### Что строится

`StampService` — сервис лояльности для брендов/кофеен/точек, где пользователи могут:

- регистрироваться через Telegram;
- быть владельцами/сотрудниками брендов;
- создавать метрики лояльности;
- начислять метрики клиентам;
- смотреть балансы и историю транзакций;
- списывать метрики;
- взаимодействовать с системой через API, Swagger и Telegram-бота.

Бизнесовая идея: метрика — это не обязательно “штамп”. Это обобщённая единица лояльности, например:

- чашки кофе;
- бонусы;
- посещения;
- условные баллы.

### Текущий статус

Основной backend уже довольно далеко продвинут:

- есть Clean Architecture;
- есть доменная модель;
- есть авторизация через Telegram login;
- есть роли и permissions;
- есть ledger-подход для балансов;
- есть выдача метрик;
- есть чтение баланса;
- есть история транзакций;
- есть Telegram bot на базе `TelegramBotFlow`;
- есть typed errors + envelope API;
- есть тесты Domain/Application/API;
- начата реализация списания метрик в Telegram-боте.

Последний активный участок работы: сценарий “Списать метрику” в Telegram-боте. Он был почти реализован, но пользователь поднял важный концептуальный вопрос: при списании сотрудник не должен вводить количество вручную. Количество для списания должно храниться в системе, вероятно в `MetricDefinition`.

### Главные задачи

1. Довести списание метрики в Telegram-боте до правильной бизнес-модели.
2. Изменить концепцию списания: количество списания должно быть задано на уровне метрики, а не вводиться сотрудником.
3. Обновить backend contract `RedeemMetricRequest`.
4. Обновить Domain/Application/API/Bot/tests под новую модель.
5. Продолжать удерживать архитектурные границы и тестовое покрытие.

---

## 2. Архитектура и ключевые решения

### Стек

- .NET 10
- C# 13
- ASP.NET Core API
- EF Core 10
- PostgreSQL
- FluentResults
- xUnit
- Telegram.Bot через кастомную библиотеку `TelegramBotFlow`
- Swagger/OpenAPI
- Clean Architecture + DDD-подход

### Структура решения

Основные проекты:

- `src/StampService.Domain`
- `src/StampService.Application`
- `src/StampService.Contracts`
- `src/StampService.Infrastructure`
- `src/StampService.API`
- `src/StampService.TelegramBot`
- `external/telegram-bot-flow`

Тесты:

- `Tests/StampService.DomainTests`
- `Tests/StampService.ApplicationTests`
- `Tests/StampService.APITests`

### Архитектурные границы

Принято как жёсткое правило:

- `StampService.Domain` не знает про Application/API/Bot/Infrastructure.
- `StampService.Application` не знает про TelegramBotFlow.
- `StampService.TelegramBot` зависит от Application и TelegramBotFlow.
- Все бизнесовые операции происходят через Application/API, бот только UI/transport.
- Telegram bot не должен содержать бизнес-логику списания/выдачи, кроме UX-валидации ввода.

### Важные технические решения

#### Permissions вместо ролей

Раньше обсуждалась роль `Customer`. От неё отказались.

Текущее понимание:

- каждый `User` потенциально является клиентом;
- owner/staff тоже могут быть клиентами;
- клиент — это не роль;
- доступы проверяются через `PermissionCode`, а не напрямую через роль.

`PermissionCode`:

- `BrandManage = 1`
- `MetricManage = 20`
- `StaffManage = 40`
- `StampIssue = 60`
- `StampRedeem = 70`
- `BalanceView = 80`

Роли сейчас:

- `OWNER`
- `STAFF`

`CUSTOMER` удалён.

#### Ledger source of truth

Принято решение:

- source of truth для операций — `stamp_transactions`;
- `metric_balances` — materialized/current state;
- при выдаче/списании создаётся транзакция и синхронно обновляется баланс;
- есть `MetricLedgerService`, который централизует ledger operation.

Это компромисс между “регистром” в стиле 1С и быстрым чтением текущего баланса.

#### Envelope + typed errors

API теперь возвращает единый формат:

- success envelope;
- error envelope.

Используется `FluentResults`, но ошибки стали typed:

- `AppError` в Application;
- `DomainError` в Domain.

API мапит оба типа в единый `ApiErrorResponse`.

Fallback `error.untyped` оставлен только как страховка для legacy/foreign `FluentResults.Error`.

#### Domain typed errors

В Domain добавлен:

- `DomainError`
- `DomainErrorType`

Доменные `Result.Fail("...")` заменены на typed domain errors.

Domain не зависит от `Application.Errors`.

#### RedemptionCode

Для безопасного списания добавлена отдельная сущность `RedemptionCode`.

Решение:

- `CustomerCode` остаётся постоянным кодом для идентификации клиента при начислении.
- `RedemptionCode` — временный одноразовый код для списания.
- списание должно идти по `RedemptionCode`, не по `UserId`.

Текущий `RedemptionCode`:

- 6 цифр;
- живёт 3 минуты;
- одноразовый;
- хранится в БД;
- имеет `UsedAtUtc`;
- `UsedAtUtc` помечен EF concurrency token;
- параллельное использование должно приводить к concurrency conflict.

#### Telegram bot library

Было обсуждение библиотек Telegram bot.

Принято:

- использовать библиотеку ментора `TelegramBotFlow`;
- подключена через `external/telegram-bot-flow`, так как NuGet пакета нет;
- `StampService.TelegramBot` зависит от `TelegramBotFlow`;
- Application/Domain не знают про TelegramBotFlow.

Rejected approach:

- не использовать `Telegram.Bot` напрямую для всего flow, потому что скоро нужны многошаговые сценарии;
- не тащить TelegramBotFlow в Application/Domain.

---

## 3. Текущее состояние

### Что уже реализовано

#### Domain

Есть основные доменные сущности:

- `User`
- `UserIdentity`
- `RedemptionCode`
- `Brand`
- `Location`
- `LocationName`
- `Address`
- `Role`
- `BrandMembership`
- `LoyaltyMetricDefinition`
- `MetricBalance`
- `StampTransaction`

Добавлен `DomainError`.

#### Application

Реализовано:

- Telegram auth service;
- ensure Telegram user;
- customer code generator;
- recipient resolver по `CustomerCode`;
- brand creation;
- assign owner;
- add staff;
- brand workspace;
- brand access service;
- metric creation;
- issue metric;
- redeem metric;
- metric balance reading;
- metric transaction history;
- user metric balances/history;
- metric ledger service;
- redemption code creation;
- redemption code usage;
- query метрик для выдачи;
- query метрик для списания, добавлен в последнем блоке.

#### Infrastructure

Есть:

- `AppDbContext`;
- EF configurations;
- repositories;
- migrations;
- role seeding;
- JWT token service.

Добавлены таблицы/миграции:

- `redemption_codes`;
- empty migration для concurrency token `UsedAtUtc`.

#### API

Есть controllers:

- `AuthController`
- `BrandsController`
- `MetricsController`
- `UsersController`
- `DevController`

Endpoints в текущем виде:

- auth/login через Telegram data;
- dev Telegram login request;
- brands create;
- assign owner;
- add staff;
- create metric;
- issue metric;
- redeem metric;
- get balance;
- get metric transactions;
- create redemption code: `POST /api/users/me/redemption-code`

`RedeemMetricRequest` сейчас уже изменён с `UserId` на `RedemptionCode`:

```csharp
public record RedeemMetricRequest(
    string RedemptionCode,
    int Amount,
    string Comment);
```

Но это уже под вопросом из-за последней мысли пользователя: `Amount` не должен вводиться сотрудником.

#### Telegram bot

Есть:

- `/start`;
- main menu;
- “Мой код”;
- “Код для списания”;
- “Мои балансы”;
- рабочие бренды;
- если один бренд — сразу кнопка конкретного бренда;
- brand workspace;
- issue metric flow;
- partial/in-progress redeem metric flow.

Issue flow:

1. выбрать бренд;
2. выбрать метрику;
3. ввести customer code;
4. ввести amount;
5. ввести comment;
6. confirm;
7. вызвать `IssueMetricCommand`.

Redeem flow начали делать:

1. выбрать бренд;
2. выбрать “Списать метрику”;
3. выбрать метрику;
4. ввести redemption code;
5. ввести amount;
6. ввести comment;
7. confirm;
8. вызвать `RedeemMetricCommand`.

Но после этого пользователь поднял концептуальный вопрос: amount при списании вводить нельзя/не нужно.

### Что сломано / в процессе

Сейчас в процессе и требует пересмотра:

- сценарий списания в Telegram bot;
- `RedeemMetricRequest.Amount`;
- `RedeemMetricEndpoint` в боте;
- `RedeemMetricAmountScreen`;
- `EnterRedeemAmountAction`;
- `RedeemMetricSessionKeys.Amount`;
- `RedeemMetricConfirmScreen`, где выводится amount;
- `RedeemMetricHandler`, который сейчас берёт amount из request.

Также был добавлен тест:

- `Tests/StampService.ApplicationTests/Metrics/GetBrandRedeemMetricsHandlerTests.cs`

После добавления этого теста тесты ещё не прогонялись, потому пользователь прервал новым вопросом.

### Known issues

1. `RedeemMetricRequest` всё ещё содержит `Amount`, но бизнесово это теперь под сомнением.
2. `LoyaltyMetricDefinition` пока не содержит настройки “сколько списывать”.
3. Нужно решить, как назвать это поле:
   - `RedeemAmount`
   - `UnitsPerRedemption`
   - `Cost`
   - `RequiredAmount`
   - `RedemptionCost`
4. В Telegram bot частично используются русские строки в нормальной кодировке, но в старых файлах есть mojibake из-за кодировки.
5. Есть технический риск с concurrency:
   - `UsedAtUtc` concurrency token добавлен;
   - API middleware мапит `DbUpdateConcurrencyException` в `409`;
   - но нужно внимательно проверить реальное поведение EF/Npgsql при параллельном использовании.
6. Бот пока не имеет полноценной автоматической тестовой инфраструктуры.

### Technical debt

- Повторение logic issue/redeem flow в боте. Можно будет позже выделить общие куски, но сейчас лучше не абстрагировать преждевременно.
- `GetBrandIssueMetrics` и `GetBrandRedeemMetrics` почти одинаковые, отличаются permission. Это осознанное дублирование ради ясности.
- Некоторые Telegram bot строки в старых файлах в mojibake. Новые файлы местами уже в читаемом UTF-8.
- `UseRedemptionCodeHandler` помечает entity как used, но не сохраняет сам. Это сделано специально для атомарности с redeem ledger operation. Нужно помнить этот contract.
- `MetricLedgerService` сохраняет через `IStampTransactionRepository.SaveAsync`. В текущей Infrastructure это один `DbContext`, поэтому вместе сохраняются tracked изменения redemption code, balance и transaction.

---

## 4. Важный контекст и договоренности

### Coding conventions

- Писать в стиле существующего проекта.
- Не делать “магические” ручные миграции, если можно через EF.
- Для миграций использовать:
  ```powershell
  dotnet ef migrations add MigrationName -p .\src\StampService.Infrastructure\StampService.Infrastructure.csproj -s .\src\StampService.API\StampService.API.csproj
  ```
- Иногда `dotnet ef migrations add` без `--no-build` даёт странный “Build failed” без ошибок. Тогда использовался workflow:
  1. `dotnet build .\src\StampService.API\StampService.API.csproj --no-restore`
  2. `dotnet ef migrations add ... --no-build`
- После миграции применяли:
  ```powershell
  dotnet ef database update -p .\src\StampService.Infrastructure\StampService.Infrastructure.csproj -s .\src\StampService.API\StampService.API.csproj --no-build
  ```

### Testing conventions

Пользователь явно попросил: после реализации и тестов делать self-review.

Правило на будущее:

1. Реализовать.
2. Прогнать релевантные тесты/сборку.
3. После зелёных тестов ещё раз пройтись по изменениям.
4. Проверить:
   - архитектурные границы;
   - бизнес-инварианты;
   - старые контракты;
   - полурабочие участки;
   - security/race conditions;
   - корректность тестов.
5. Если найден недочёт по текущей задаче — исправить сразу.
6. После исправления снова прогнать тесты/сборку.

### Commands

Часто использовались:

```powershell
$env:DOTNET_CLI_HOME='C:\Programmer\StampService\StampService\.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet test StampService.sln --no-restore -m:1 -p:UseSharedCompilation=false
```

Для полной сборки без конфликта с запущенными API/ботом:

```powershell
$env:DOTNET_CLI_HOME='C:\Programmer\StampService\StampService\.dotnet-cli'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
dotnet build StampService.sln --no-restore -m:1 -p:UseSharedCompilation=false -p:OutputPath=C:\Programmer\StampService\StampService\obj\codex-build\solution\
```

### API contracts

#### Issue

Current issue request:

```csharp
public record IssueMetricRequest(
    Guid UserId,
    int Amount,
    string Comment);
```

Issue still uses `UserId`, because recipient is resolved by `CustomerCode` in bot/application.

#### Redeem

Current redeem request before conceptual correction:

```csharp
public record RedeemMetricRequest(
    string RedemptionCode,
    int Amount,
    string Comment);
```

Likely next change:

```csharp
public record RedeemMetricRequest(
    string RedemptionCode,
    string Comment);
```

Amount should come from `MetricDefinition`.

### Assumptions

- Every `User` can be a customer.
- Customer identity for issue can be via persistent `CustomerCode`.
- Customer confirmation for redeem must be via temporary one-time `RedemptionCode`.
- Staff/owner actions must be permission-based.
- Telegram bot is just an interface.

### Ограничения

- Не тянуть TelegramBotFlow в Application/Domain.
- Не оправдывать плохие решения “MVP”.
- Не делать ручные EF migration edits без причины.
- Не использовать role membership вместо permissions, когда проверяется действие.
- Не списывать по постоянному customer code.

---

## 5. Все важные сущности

### Domain classes

#### User

`src/StampService.Domain/User/User.cs`

- `Name`
- `CustomerCode`
- identities
- creates 4-digit customer code
- validates customer code

`CustomerCode` — постоянный код, сейчас используется для начисления.

#### UserIdentity

`src/StampService.Domain/User/UserIdentity.cs`

- identity type;
- key;
- metadata.

#### RedemptionCode

`src/StampService.Domain/User/RedemptionCode.cs`

Fields:

- `UserId`
- `User`
- `Code`
- `ExpiresAtUtc`
- `UsedAtUtc`

Rules:

- code length = 6;
- digits only;
- expiration must be in future;
- cannot use twice;
- cannot use expired;
- `IsActive(nowUtc)` = not used and not expired.

#### Brand

`src/StampService.Domain/Brand/Brand.cs`

Brand root.

#### Role

`src/StampService.Domain/Access/Role.cs`

Roles:

- owner;
- staff.

#### BrandMembership

`src/StampService.Domain/Access/BrandMembership.cs`

Connects user-brand-role.

#### LoyaltyMetricDefinition

`src/StampService.Domain/Loyalty/LoyaltyMetricDefinition.cs`

Current fields:

- `BrandId`
- `Code`
- `Name`
- `IsActive`

Likely next field:

- `RedemptionAmount` / `RedeemAmount` / `RedemptionCost`

This is the key next domain change.

#### MetricBalance

`src/StampService.Domain/Loyalty/MetricBalance.cs`

Fields:

- `UserId`
- `BrandId`
- `MetricDefinitionId`
- `Value`

Methods:

- `Add(amount)`
- `Subtract(amount)`
- `SetMaterializedValue(value)`

#### StampTransaction

`src/StampService.Domain/Loyalty/StampTransaction.cs`

Fields:

- `MetricBalanceId`
- `Type`
- `Amount`
- `Comment`

Types:

- `Issue`
- `Redeem`

### Application services/modules

#### Auth

- `AuthService`
- `TelegramValidationService`
- `JwtTokenService`

#### Users

- `EnsureTelegramUserHandler`
- `CustomerCodeGenerator`
- `RecipientResolver`
- `CreateRedemptionCodeHandler`
- `UseRedemptionCodeHandler`
- `RedemptionCodeGenerator`

Important contract:

`UseRedemptionCodeHandler` marks code used but does not save. Caller’s unit of work must save.

#### Brands

- `CreateBrandHandler`
- `AssignBrandOwnerHandler`
- `AddBrandStaffHandler`
- `GetMyBrandsHandler`
- `GetBrandWorkspaceHandler`
- `BrandAccessService`
- `BrandMembershipService`

#### Metrics

- `CreateMetricHandler`
- `IssueMetricHandler`
- `RedeemMetricHandler`
- `MetricLedgerService`
- `GetMetricBalanceHandler`
- `GetMetricTransactionsHandler`
- `GetUserMetricBalancesHandler`
- `GetUserMetricTransactionsHandler`
- `GetBrandIssueMetricsHandler`
- `GetBrandRedeemMetricsHandler`

### Tables

Current important tables:

- `users`
- `user_identities`
- `brands`
- `locations`
- `roles`
- `brand_memberships`
- `loyalty_metric_definitions`
- `metric_balances`
- `stamp_transactions`
- `redemption_codes`

`redemption_codes` columns:

- `id`
- `user_id`
- `code`
- `expires_at_utc`
- `used_at_utc`
- `created_at`
- `updated_at`
- `deleted_at`

Indexes:

- `IX_redemption_codes_code`
- `IX_redemption_codes_user_id_used_at_utc_expires_at_utc`

### Endpoints

Known API endpoints:

#### Auth

- login via Telegram data.

#### Brands

- `POST /api/Brands`
- `POST /api/Brands/{brandId}/owner`
- `POST /api/Brands/{brandId}/staff`

#### Metrics

- `POST /api/brands/{brandId}/metrics`
- `POST /api/metrics/{metricDefinitionId}/issue`
- `POST /api/metrics/{metricDefinitionId}/redeem`
- `GET /api/metrics/{metricDefinitionId}/balances/{userId}`
- `GET /api/metrics/{metricDefinitionId}/transactions?userId=...&skip=...&take=...`

#### Users

- `POST /api/users/me/redemption-code`

### Telegram workflows

#### Main menu

Shows:

- `Мой код`
- `Код для списания`
- `Мои балансы`
- brand button if one brand;
- `Рабочие бренды` if multiple brands;
- no brand button if no working brands.

#### Issue metric workflow

State keys:

- `issue_metric.metric_definition_id`
- `issue_metric.metric_name`
- `issue_metric.recipient_user_id`
- `issue_metric.recipient_customer_code`
- `issue_metric.amount`
- `issue_metric.comment`

Flow:

1. select metric;
2. enter customer code;
3. enter amount;
4. enter comment;
5. confirm/cancel;
6. call Application.

#### Redeem metric workflow, currently in progress

State keys added:

- `redeem_metric.metric_definition_id`
- `redeem_metric.metric_name`
- `redeem_metric.redemption_code`
- `redeem_metric.amount`
- `redeem_metric.comment`

But amount state key is likely wrong after latest conceptual change.

Flow currently implemented but not finalized:

1. select metric;
2. enter redemption code;
3. enter amount;
4. enter comment;
5. confirm/cancel;
6. call `RedeemMetricCommand`.

Need revise to:

1. select metric;
2. enter redemption code;
3. enter comment;
4. confirm/cancel;
5. call `RedeemMetricCommand` with amount from metric definition.

---

## 6. Последние изменения

### Что делали в последних сообщениях

1. Added `RedemptionCode` support.
2. Changed redeem backend to use `RedemptionCode` instead of `UserId`.
3. Added Telegram bot button “Код для списания”.
4. Added self-review rule.
5. Self-review found and fixed:
   - code was saved as used before redeem operation;
   - fixed by not saving in `UseRedemptionCodeHandler`;
   - added concurrency token for `UsedAtUtc`;
   - mapped `DbUpdateConcurrencyException` to HTTP `409`.
6. Started implementing Telegram bot redeem flow.
7. Added `GetBrandRedeemMetricsQuery/Handler`.
8. Added Telegram bot `RedeemMetric` feature files:
   - actions;
   - screens;
   - endpoint.
9. Replaced brand workspace redeem placeholder with navigation to `RedeemMetricSelectScreen`.
10. Added test:
    - `GetBrandRedeemMetricsHandlerTests`.

### Какие гипотезы проверяли

- Reuse `GetBrandIssueMetrics` for redeem selection?
  - Rejected because it checks `StampIssue`, not `StampRedeem`.
- Use `UserId` in redeem request?
  - Rejected because redemption must be confirmed by temporary code.
- Save redemption code usage immediately?
  - Rejected after self-review because it can burn code without successful ledger operation.
- Let concurrency exception become 500?
  - Rejected; should map to `409`.

### Что оказалось неверным

The current in-progress Telegram redeem flow assumes employee enters amount.

User pointed out this is likely wrong:

> Для списания метрик не должно указывать сколько списать. В системе должно храниться сколько единиц для списания требуется. Наверное в metric definition.

This is a correct business insight.

### Что осталось доделать прямо сейчас

Need stop and redesign redeem amount:

1. Add redeem amount/cost to `LoyaltyMetricDefinition`.
2. Add EF migration.
3. Update create metric request/handler/domain tests.
4. Update redeem handler to use metric’s configured redeem amount.
5. Remove amount from `RedeemMetricRequest`.
6. Remove amount input step from Telegram redeem flow.
7. Update confirmation screen to show configured amount.
8. Update tests.

---

## 7. Pending tasks

Priority order:

1. Resolve conceptual change: define exact field name and rule for metric redeem amount.
   - Suggested name: `RedemptionAmount` or `RedeemAmount`.
   - Need decide whether it means “how many units are deducted per one redemption”.
2. Update `LoyaltyMetricDefinition`:
   - add property;
   - validate > 0;
   - include in create/update logic if needed.
3. Update Contracts:
   - `CreateMetricRequest` should include redemption amount.
   - `MetricResponse` probably should include redemption amount.
   - `RedeemMetricRequest` should remove `Amount`.
4. Update Application:
   - `CreateMetricHandler`;
   - `RedeemMetricHandler`;
   - `GetBrandIssueMetricsHandler`;
   - `GetBrandRedeemMetricsHandler`;
   - tests.
5. Create EF migration for new column in `loyalty_metric_definitions`.
6. Apply migration.
7. Update Telegram bot redeem flow:
   - delete/ignore `RedeemMetricAmountScreen`;
   - remove `EnterRedeemAmountAction`;
   - remove amount session key;
   - after redemption code go directly to comment;
   - confirmation shows amount from selected metric definition/session.
8. Decide how bot obtains amount:
   - store selected metric’s `RedemptionAmount` in session from `MetricResponse`;
   - or re-query metric on confirm.
   Recommended: store for UX display, but backend still uses authoritative metric definition.
9. Update tests:
   - Domain tests for metric definition redeem amount;
   - Application tests for redeem uses metric amount;
   - Telegram compile check.
10. Run:
   - all tests;
   - full build.
11. Do self-review.

---

## 8. Риски и проблемы

### Potential bugs

1. Current partial redeem bot flow may compile, but business flow is wrong because it asks amount.
2. If `RedeemMetricRequest.Amount` remains, staff can choose arbitrary amount and bypass intended business rule.
3. If `MetricDefinition` stores redemption amount, existing metrics need migration/backfill default.
4. Need decide default value for existing rows:
   - likely `1`;
   - but if existing metrics were created as arbitrary units, this is a business assumption.
5. If redemption amount is configurable, need decide whether staff can update it later.
6. If update allowed, need think about historical transactions:
   - transaction stores actual amount, so changing future redeem amount is okay.
7. Concurrency token helps, but race behavior should eventually be integration-tested with real DB.
8. Bot screens may contain mojibake in old files. New files use readable Russian.
9. `UseRedemptionCodeHandler` not saving can surprise future developer. There is a comment now, but new chat should preserve this invariant.

### Спорные места

#### Where should redemption amount live?

Likely in `LoyaltyMetricDefinition`.

Potential names:

- `RedeemAmount`
- `RedemptionAmount`
- `RedemptionCost`
- `RequiredAmountForRedemption`

Best practical name: `RedemptionAmount`.

Reason:

- it is amount deducted from balance during redemption;
- clear enough;
- aligns with `RedemptionCode`.

#### Should issue amount also be configured?

Not currently. Issue amount is still employee input, because начисление может быть разным.

#### Should redemption code be brand-specific or metric-specific?

Currently no. It is user-level and can be used for any redeem operation within expiration. That means if user shows code, staff can redeem any metric they have permission to redeem. Later possible hardening:

- code tied to brand;
- code tied to metric;
- code tied to max amount;
- code generated from specific “redeem this reward” action.

For now accepted: user-level one-time code.

### Где модель сомневалась

- Whether to save redemption code inside `UseRedemptionCodeHandler`. Self-review concluded no.
- Whether to create empty migration for concurrency token. It was created and applied.
- Whether to reuse issue metric query. Rejected.
- Whether Telegram bot should resolve redemption code to user before confirm. Rejected; backend should do that.

---

## 9. Инструкции для нового чата

### Как продолжить без потери контекста

Start by acknowledging the latest user insight:

> Для списания метрик не должно указывать сколько списать; количество должно храниться в metric definition.

Do not continue current bot implementation blindly. First adjust backend model.

Recommended next steps:

1. Inspect these files:
   - `src/StampService.Domain/Loyalty/LoyaltyMetricDefinition.cs`
   - `src/StampService.Contracts/DTOs/Metrics/CreateMetricRequest.cs`
   - `src/StampService.Contracts/DTOs/Metrics/MetricResponse.cs`
   - `src/StampService.Contracts/DTOs/Metrics/RedeemMetricRequest.cs`
   - `src/StampService.Application/Metrics/Commands/CreateMetric/CreateMetricHandler.cs`
   - `src/StampService.Application/Metrics/Commands/RedeemMetric/RedeemMetricHandler.cs`
   - `src/StampService.TelegramBot/Features/RedeemMetric`
   - `src/StampService.Infrastructure/Configurations/LoyaltyMetricDefinitionConfiguration.cs`
2. Add redemption amount to metric definition.
3. Remove amount input from redeem flow.
4. Update tests.
5. Run all tests/build.
6. Do self-review.

### На чём НЕ терять время повторно

- Не обсуждать снова, нужна ли `Customer` role. Она не нужна.
- Не обсуждать снова, можно ли проверять доступы по roles напрямую. Нужно через permissions.
- Не обсуждать снова, должен ли бот зависеть от TelegramBotFlow. Да, только bot project.
- Не обсуждать снова, нужен ли temporary code для списания. Да, `RedemptionCode` уже добавлен.
- Не возвращать `UserId` в `RedeemMetricRequest`.
- Не делать списание по `CustomerCode`.
- Не сохранять `RedemptionCode` used отдельно до ledger operation.
- Не заменять ledger source of truth на direct balance-only mutation.

### Файлы, которые важно проверить после продолжения

- `src/StampService.TelegramBot/Features/RedeemMetric/Endpoints/RedeemMetricEndpoint.cs`
- `src/StampService.TelegramBot/Features/RedeemMetric/Screens/RedeemMetricAmountScreen.cs`
- `src/StampService.TelegramBot/Features/RedeemMetric/Actions/EnterRedeemAmountAction.cs`
- `src/StampService.TelegramBot/Features/RedeemMetric/RedeemMetricSessionKeys.cs`
- `src/StampService.Application/Metrics/Commands/RedeemMetric/RedeemMetricHandler.cs`
- `src/StampService.Contracts/DTOs/Metrics/RedeemMetricRequest.cs`

These are currently aligned to old idea “staff enters amount” and must be corrected.

---

## 10. Additional Implementation Notes

### Current test counts before partial redeem-bot additions

Before the last in-progress bot redeem additions, tests were:

- Domain: 37
- Application: 60
- API: 13
- Total: 110

After adding `GetBrandRedeemMetricsHandlerTests`, expected total should increase by 2, but tests were not run after this exact addition because user interrupted with conceptual correction.

### Recent successful commands

Before bot redeem partial work:

```powershell
dotnet test StampService.sln --no-restore -m:1 -p:UseSharedCompilation=false
```

passed `110/110`.

```powershell
dotnet build StampService.sln --no-restore -m:1 -p:UseSharedCompilation=false -p:OutputPath=C:\Programmer\StampService\StampService\obj\codex-build\solution\
```

passed.

During partial bot redeem work:

- initial build failed due namespace ambiguity between `RedemptionCode` feature namespace and domain class;
- fixed with aliases:
  - `DomainRedemptionCode`
  - `LoyaltyConstants`
- build then passed before adding the latest test file.

### Partial files added for redeem bot

Added:

- `src/StampService.Application/Metrics/Queries/GetBrandRedeemMetrics/GetBrandRedeemMetricsQuery.cs`
- `src/StampService.Application/Metrics/Queries/GetBrandRedeemMetrics/GetBrandRedeemMetricsHandler.cs`
- `src/StampService.TelegramBot/Features/RedeemMetric/RedeemMetricSessionKeys.cs`
- `src/StampService.TelegramBot/Features/RedeemMetric/Actions/*`
- `src/StampService.TelegramBot/Features/RedeemMetric/Screens/*`
- `src/StampService.TelegramBot/Features/RedeemMetric/Endpoints/RedeemMetricEndpoint.cs`
- `Tests/StampService.ApplicationTests/Metrics/GetBrandRedeemMetricsHandlerTests.cs`

Modified:

- `src/StampService.TelegramBot/Features/Brands/Screens/BrandWorkspaceScreen.cs`

Caution: these are based on now-questioned amount-input design.

---

## 11. Краткий bootstrap summary

Мы строим `StampService`: .NET 10 Clean Architecture сервис лояльности с API и Telegram-ботом. Пользователи регистрируются через Telegram, могут быть owner/staff брендов, создавать метрики, начислять/списывать их клиентам, смотреть балансы и историю. Клиент — любой `User`, роли `Customer` нет. Доступы проверяются через `PermissionCode`, не напрямую через роли.

Баланс построен как ledger: source of truth — `stamp_transactions`, `metric_balances` — materialized актуальное состояние. `MetricLedgerService` централизует issue/redeem и синхронизацию баланса. API возвращает единый envelope, ошибки typed: `AppError` в Application и `DomainError` в Domain.

Для начисления используется постоянный `CustomerCode` 4 цифры. Для списания добавлен `RedemptionCode`: 6 цифр, 3 минуты, одноразовый. `RedeemMetricRequest` был изменён на `RedemptionCode, Amount, Comment`, но пользователь только что правильно заметил, что сотрудник не должен вводить amount при списании. Amount должен храниться в `MetricDefinition`. Значит следующий шаг — добавить в `LoyaltyMetricDefinition` поле вроде `RedemptionAmount`, убрать `Amount` из `RedeemMetricRequest` и из Telegram redeem flow, а `RedeemMetricHandler` должен брать amount из metric definition.

Telegram bot использует `external/telegram-bot-flow`; только bot project зависит от него. Уже есть main menu, “Мой код”, “Код для списания”, issue flow. Redeem flow был частично добавлен, но его нужно переделать под новую бизнес-модель: выбрать метрику → ввести redemption code → comment → confirm → backend списывает configured redemption amount. После любых правок обязательно прогнать тесты/сборку и сделать self-review.