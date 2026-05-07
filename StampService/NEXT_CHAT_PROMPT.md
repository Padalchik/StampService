# StampService: итоговый промт для следующего ИИ

Ты - Codex / Senior .NET Engineer. Мы работаем над проектом `StampService`.

Рабочая папка:

```text
C:\Programmer\StampService\StampService
```

Отвечай по-русски. Пользователь хочет понимать архитектурные решения, поэтому если задача затрагивает концепцию, границы слоев, БД, права доступа или модель данных, сначала объясни подход и только потом вноси изменения. Не оправдывай объективно плохие решения тем, что это MVP.

## 1. Бизнес-контекст

`StampService` - SaaS loyalty service.

Система нужна для брендов, которые выдают пользователям метрики лояльности: штампы, баллы, бонусы и т.п. Сейчас основная метрика - "штампы", но модель специально названа шире: `LoyaltyMetricDefinition`, `MetricBalance`, `StampTransaction`.

Клиентом считается любой `User`.

Отдельной роли `CUSTOMER` больше нет. Владелец и сотрудник тоже могут быть получателями метрик. Если пользователю выдают метрику, а его баланса по этой метрике еще нет, баланс создается автоматически.

Роли сейчас:

```text
OWNER
STAFF
```

`OWNER` управляет брендом, метриками и сотрудниками.

`STAFF` может выдавать/списывать метрики и смотреть балансы/историю, но не может создавать метрики и добавлять сотрудников.

## 2. Технический стек

- .NET 10
- C# 13
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Docker
- xUnit
- Clean Architecture + DDD

Слои:

```text
src/StampService.Domain
src/StampService.Application
src/StampService.Contracts
src/StampService.Infrastructure
src/StampService.API
Tests/StampService.DomainTests
Tests/StampService.ApplicationTests
```

## 3. Архитектурные правила

Бизнес-логика должна быть в `Application`.

`Infrastructure` не должна принимать бизнес-решения. Она реализует persistence, JWT generation и другие технические детали.

`Application` зависит от repository-интерфейсов, а не от `AppDbContext`.

`AppDbContext` используется в `Infrastructure`, а также в `API` только для startup/migrations/seeding.

Контроллеры должны быть тонкими:

- получают request
- достают route/body/JWT данные
- создают command/query
- вызывают handler/service
- маппят результат в HTTP response

Не писать EF-запросы в контроллерах.

Не писать бизнес-логику в контроллерах.

Предпочитать текущие паттерны проекта, не добавлять лишние абстракции.

## 4. Domain

Основные сущности:

- `User`
- `UserIdentity`
- `Brand`
- `Location`
- `Role`
- `BrandMembership`
- `LoyaltyMetricDefinition`
- `MetricBalance`
- `StampTransaction`

`CUSTOMER` удален и не должен возвращаться без отдельного архитектурного обсуждения.

## 5. PermissionCode

Текущие permission codes:

```csharp
public enum PermissionCode
{
    BrandManage = 1,
    MetricManage = 20,
    StaffManage = 40,
    StampIssue = 60,
    StampRedeem = 70,
    BalanceView = 80
}
```

`PermissionCode` - это не роли. Это действия/возможности внутри бренда.

`BrandAccessService` сейчас содержит hardcoded mapping:

```text
OWNER => true

STAFF => StampIssue, StampRedeem, BalanceView

unknown/no membership => false
```

В будущем hardcoded mapping можно заменить таблицами `role_permissions`, не меняя контроллеры/handlers, потому что они уже спрашивают "может ли пользователь выполнить действие?", а не "какая у пользователя роль?".

## 6. Auth / JWT / Telegram

Есть endpoint:

```http
POST /api/auth/telegram
```

DTO:

- `TelegramLoginRequest`
- `AuthResponse`

Flow:

- `AuthController` вызывает `IAuthService.LoginAsync(...)`
- `AuthService` находится в `Application`
- `AuthService` валидирует Telegram request через `ITelegramValidationService`
- ищет `User` по Telegram identity
- если user не найден, создает `User` и `UserIdentity`
- вызывает `IJwtTokenService` для выпуска JWT

`IJwtTokenService` находится в `Application`, реализация `JwtTokenService` - в `Infrastructure`.

JWT содержит claim:

```csharp
ClaimTypes.NameIdentifier = user.Id
```

Важно: `TelegramValidationService` сейчас небезопасная заглушка. Она проверяет только наличие `hash`.

Перед реальной интеграцией с Telegram нужно сделать:

- `Telegram:BotToken` в server config
- HMAC-SHA256 проверку hash
- проверку TTL по `auth_date`
- выдавать JWT только после валидного Telegram hash

Правильное правило:

```text
Telegram login data -> backend validates hash with bot token -> only then issue JWT
```

## 7. Docker / DB / EF

Postgres в Docker:

```text
container: stamp_service_postgres
database: stamp_service
port: 5440
user: postgres
password: postgres
```

Development connection string должен смотреть на:

```text
Host=localhost;Port=5440;Database=stamp_service;Username=postgres;Password=postgres
```

Применение миграций:

```powershell
cd C:\Programmer\StampService\StampService\src
dotnet ef database update -p .\StampService.Infrastructure -s .\StampService.API
```

Обычно пользователь создает миграции командой:

```powershell
dotnet ef migrations add MigrationName -p .\StampService.Infrastructure -s .\StampService.API
```

Сборка:

```powershell
dotnet build StampService.sln
```

Все тесты:

```powershell
dotnet test StampService.sln
```

## 8. DI

Application регистрирует:

- command/query handlers через Scrutor
- `IAuthService`
- `IBrandAccessService`
- `IBrandMembershipService`
- `IMetricLedgerService`
- `ITelegramValidationService`

Infrastructure регистрирует:

- `AppDbContext`
- `IBrandRepository`
- `IUserRepository`
- `IBrandMembershipRepository`
- `ILoyaltyMetricRepository`
- `IMetricBalanceRepository`
- `IStampTransactionRepository`
- `IJwtTokenService`

## 9. Реализованные endpoints

### Auth

```http
POST /api/auth/telegram
```

Логин через Telegram-заглушку, выдача JWT.

### Brands

```http
POST /api/Brands
```

Создает бренд.

Важно:

- creator не становится owner автоматически
- owner назначается отдельным endpoint

### Assign Owner

```http
POST /api/Brands/{brandId}/owner
```

Body:

```json
{
  "userId": "USER_GUID"
}
```

Правила:

- если brand не существует - ошибка
- если user не существует - ошибка
- если у бренда уже есть другой owner - `"Brand already has an owner"`
- если owner уже тот же user - запрос идемпотентен
- если membership user+brand уже есть - роль меняется на `OWNER`
- если membership нет - создается `BrandMembership`

DB-level unique constraint на одного owner на бренд пока не добавлен.

### Add Staff

```http
POST /api/Brands/{brandId}/staff
```

Body:

```json
{
  "userId": "USER_GUID"
}
```

Request user берется из JWT.

Требуется `PermissionCode.StaffManage`.

Правила:

- `OWNER` может добавить сотрудника
- `STAFF` не может добавить сотрудника
- если target membership отсутствует - создать `STAFF`
- если target membership существует - сменить роль на `STAFF`
- если target user является `OWNER` - вернуть `"Cannot change owner role"`

Роль в body пока не передается осознанно: endpoint именно добавляет сотрудника.

### Create Metric

```http
POST /api/brands/{brandId}/metrics
```

Создает определение метрики лояльности.

Требуется `PermissionCode.MetricManage`.

`OWNER` может, `STAFF` не может.

DTO:

- `CreateMetricRequest`
- `MetricResponse`

`code` уникален в рамках brand.

### Issue Metric

```http
POST /api/metrics/{metricDefinitionId}/issue
```

Body:

```json
{
  "userId": "USER_GUID",
  "amount": 1,
  "comment": "optional"
}
```

В route только `metricDefinitionId`. `brandId` извлекается из самой метрики.

Требуется `PermissionCode.StampIssue`.

`OWNER` и `STAFF` могут.

Если `MetricBalance` для user+metric отсутствует, он создается автоматически.

Создается `StampTransaction` типа `Issue`.

`StampTransaction.Amount` всегда положительный.

### Redeem Metric

```http
POST /api/metrics/{metricDefinitionId}/redeem
```

Body:

```json
{
  "userId": "USER_GUID",
  "amount": 1,
  "comment": "optional"
}
```

Требуется `PermissionCode.StampRedeem`.

Баланс должен существовать или быть синхронизирован.

Если средств недостаточно - ошибка.

Создается `StampTransaction` типа `Redeem`.

`StampTransaction.Amount` всегда положительный.

### Read Balance

```http
GET /api/metrics/{metricDefinitionId}/balances/{userId}
```

Возвращает materialized balance.

Требуется `PermissionCode.BalanceView`.

Если баланса нет, возвращается значение `0` и `balanceId: null`. DB row при чтении не создается.

### Transaction History

```http
GET /api/metrics/{metricDefinitionId}/transactions?userId=USER_GUID&skip=0&take=50
```

Требуется `PermissionCode.BalanceView`.

Параметры:

- `skip` по умолчанию `0`
- `take` по умолчанию `50`
- максимальный `take` - `100`

Если баланса нет, возвращается пустой список.

Метод репозитория называется:

```csharp
GetHistoryByMetricBalanceAsync
```

Не возвращать название `GetByBalanceAsync`, оно было признано вводящим в заблуждение.

## 10. Ledger model

Принята строгая модель:

```text
stamp_transactions = source of truth
metric_balances = materialized current balance
```

`MetricBalance.Value` не является самостоятельной бизнес-истиной.

Он обновляется только через ledger-сценарии и может быть пересчитан из `stamp_transactions`.

`MetricLedgerService` находится в `Application` и нужен, чтобы централизовать ledger mechanics.

Он содержит:

- `IssueAsync`
- `RedeemAsync`
- `RecalculateMetricBalanceAsync`

Зачем нужен `MetricLedgerService`:

- чтобы выдача, списание и пересчет баланса жили в одном месте
- чтобы разные handlers не дублировали правила
- чтобы `metric_balances` всегда синхронизировался через один механизм
- чтобы `stamp_transactions` оставался источником правды

Перед issue/redeem существующий balance синхронизируется с суммой транзакций.

Типы транзакций:

```csharp
public enum StampTransactionType
{
    Issue = 1,
    Redeem = 2
}
```

`StampTransaction.Amount` всегда положительный.

Смысл операции определяется через `TransactionType`.

DB constraints:

- `stamp_transactions.amount > 0`
- `stamp_transactions.transaction_type IN (1, 2)`
- `metric_balances.value >= 0`

Ранее из `stamp_transactions` удалено redundant поле `metric_definition_id`.

Теперь metric definition узнается так:

```text
stamp_transactions.metric_balance_id
  -> metric_balances.metric_definition_id
```

Это нормализация. Если нужно получить definition, делается join.

## 11. Тесты

Пользователь хотел сначала понять тестовую инфраструктуру, затем делать.

Мы начали с unit tests.

### Domain unit tests

Проект:

```text
Tests/StampService.DomainTests
```

Без БД, Docker, API, Infrastructure.

In-memory only.

Тесты:

- `MetricBalanceTests`
- `StampTransactionTests`
- `LoyaltyMetricDefinitionTests`
- `BrandMembershipTests`

### Application unit tests

Проект:

```text
Tests/StampService.ApplicationTests
```

Без БД, Infrastructure, API.

Используются fake repositories:

- `FakeBrandMembershipRepository`
- `FakeMetricBalanceRepository`
- `FakeStampTransactionRepository`

Тесты:

- `BrandAccessServiceTests`
- `MetricLedgerServiceTests`

Оба тестовых проекта используют:

- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`

Оба проекта добавлены в `StampService.sln`.

Последний известный результат:

```text
DomainTests: 23 passed
ApplicationTests: 10 passed
Total: 33 passed
```

Запуск всех тестов:

```powershell
dotnet test StampService.sln
```

В VS Code тесты можно запускать через Testing panel при установленном C# Dev Kit.

## 12. Важные технические долги

### Telegram validation

Сейчас Telegram validation небезопасная заглушка.

Перед реальным ботом обязательно сделать настоящую hash validation.

### Error mapping

Сейчас error mapping местами примитивный: `BadRequest(result.Errors)`.

Позже стоит нормализовать ошибки:

- validation/domain errors -> 400
- access denied -> 403
- not found -> 404
- conflict -> 409

### Owner uniqueness

Нужна DB-level гарантия "один OWNER на бренд", если появятся параллельные запросы.

Сейчас это правило реализовано на уровне Application.

### Transaction editing

Концепция допускает, что `stamp_transactions` - источник правды, а `metric_balances` пересчитывается при изменении ledger.

Сейчас реализованы выдача/списание и пересчет. Если позже появится редактирование/откат транзакций, оно должно идти через ledger-сервис или отдельный application scenario, который затем пересчитает `MetricBalance`.

## 13. Стиль дальнейшей работы

Отвечай по-русски.

Перед изменениями сначала изучай текущий код.

Если задача явно на реализацию - реализуй сам, затем запускай build/tests.

Если задача архитектурная - сначала объясни вариант, последствия и только потом делай.

Не возвращай `CUSTOMER` без отдельного обсуждения.

Не добавляй сложную permission DB-систему раньше времени, но и не делай плохой дизайн только потому, что "это MVP".

Не пиши EF в контроллерах.

Не перемещай бизнес-логику в Infrastructure.

Не откатывай пользовательские изменения без прямой просьбы.

Используй существующие паттерны проекта.

## 14. Возможные следующие шаги

Логичные направления после текущего состояния:

1. Расширять покрытие Application unit tests для handlers выдачи, списания, чтения баланса и истории.
2. Добавить integration tests позже, отдельно обсудив тестовую БД и стратегию сброса состояния.
3. Реализовать настоящую Telegram validation перед подключением Telegram bot.
4. Добавить endpoint/сценарии отмены или корректировки транзакции, если бизнесу нужен rollback.
5. Добавить DB-level unique constraint на одного owner на бренд.
6. Нормализовать error mapping.

