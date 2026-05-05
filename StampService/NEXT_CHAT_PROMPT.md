# StampService Next Chat Prompt

Ты - Codex / Senior .NET Engineer. Мы работаем над проектом StampService.

Проект:
- SaaS loyalty service.
- .NET 10 / C# 13.
- Clean Architecture + DDD.
- Слои:
  - StampService.Domain
  - StampService.Application
  - StampService.Contracts
  - StampService.Infrastructure
  - StampService.API
- Рабочая папка:
  C:\Programmer\StampService\StampService

Стиль архитектуры:
- Бизнес-логика должна быть в Application.
- Infrastructure не должна принимать бизнес-решения.
- Infrastructure реализует persistence, JWT generation и внешние технические детали.
- Application зависит от repository-интерфейсов, а не от AppDbContext.
- AppDbContext используется только в Infrastructure/API startup/migrations/seeding.
- Контроллеры должны быть тонкими:
  - получают request
  - достают route/body/JWT данные
  - создают command/query
  - вызывают handler/service
  - маппят результат в HTTP response
- Не писать EF-запросы в контроллерах.
- Не писать бизнес-логику в контроллерах.
- Предпочитать текущие паттерны проекта, не городить лишние абстракции.
- YAGNI: не добавлять кэширование, refresh token, сложную permission DB-систему, policy-based authorization ASP.NET без необходимости.

Текущее состояние проекта:

1. Domain

Есть сущности:
- User
- UserIdentity
- Brand
- Location
- Role
- BrandMembership
- LoyaltyMetricDefinition
- MetricBalance
- StampTransaction

Роли:
- SystemRoles.Owner = "OWNER"
- SystemRoles.Staff = "STAFF"
- SystemRoles.Customer = "CUSTOMER"

PermissionCode сейчас такой:

```csharp
public enum PermissionCode
{
    BrandManage = 1,
    MetricManage = 20,
    StaffManage = 40,
    StampIssue = 60,
    BalanceView = 80
}
```

Смысл PermissionCode:
- Это не роли.
- Это действия/возможности внутри бренда.
- Для MVP права захардкожены в Application.
- В будущем hardcoded mapping можно заменить таблицами role_permissions, не меняя контроллеры/handlers.

2. JWT + Telegram Auth

JWT авторизация уже работает.

Endpoint:

```http
POST /api/auth/telegram
```

DTO:
- TelegramLoginRequest
- AuthResponse

Flow:
- AuthController вызывает IAuthService.LoginAsync(...)
- AuthService находится в Application.
- AuthService:
  - валидирует Telegram request через ITelegramValidationService
  - ищет User по Telegram identity
  - если user не найден, создает User и UserIdentity
  - вызывает IJwtTokenService для выпуска JWT
- IUserRepository находится в Application.
- UserRepository находится в Infrastructure и работает с AppDbContext.
- JwtTokenService находится в Infrastructure, потому что это техническая генерация JWT.
- JWT содержит claim:

```csharp
ClaimTypes.NameIdentifier = user.Id
```

ВАЖНО:
TelegramValidationService сейчас MVP-заглушка. Она проверяет только наличие hash.
Это небезопасно для production.

Когда начнем делать реальный Telegram bot/auth, первым делом внедрить настоящую Telegram hash validation:
- Telegram:BotToken в server config
- HMAC-SHA256 проверка hash
- проверка auth_date TTL
- JWT выдавать только после валидного Telegram hash

Архитектурное правило:

```text
Telegram login data -> backend validates hash with bot token -> only then issue JWT
```

3. OpenAPI / Swagger

Swagger UI настроен.
Есть кнопка Authorize для Bearer JWT.
OpenAPI extension вынесен в API Extensions.
В Swagger JWT вставлять без префикса Bearer.

4. Docker / DB

Есть compose.yaml в корне проекта.
Postgres в Docker:

```yaml
services:
  postgres:
    container_name: stamp_service_postgres
    image: postgres
    restart: always
    environment:
      POSTGRES_DB: stamp_service
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5440:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data

volumes:
  postgres-data:
```

Development connection string уже должен быть:

```json
"DefaultConnection": "Host=localhost;Port=5440;Database=stamp_service;Username=postgres;Password=postgres"
```

Если миграции не появляются в Docker DB, проверить, что appsettings.Development.json смотрит на:
- localhost
- port 5440
- database stamp_service

Для EF:

```powershell
cd C:\Programmer\StampService\StampService\src
dotnet ef database update --project .\StampService.Infrastructure\StampService.Infrastructure.csproj --startup-project .\StampService.API\StampService.API.csproj
```

Если build падает из-за locked dll:
- остановить запущенный API/debug session
- процесс может называться StampService.API или Visual Studio Debug Adapter
- ошибка вида "file is being used by another process" не связана с БД

5. DI / handlers

Application DI:
- регистрирует ICommandHandler<,>
- регистрирует IQueryHandler<,>
- регистрирует Application services:
  - IAuthService -> AuthService
  - IBrandAccessService -> BrandAccessService
  - IBrandMembershipService -> BrandMembershipService
  - ITelegramValidationService -> TelegramValidationService

Infrastructure DI:
- AddDbContext<AppDbContext>
- регистрирует repositories:
  - IBrandRepository -> BrandRepository
  - IUserRepository -> UserRepository
  - IBrandMembershipRepository -> BrandMembershipRepository
- регистрирует technical service:
  - IJwtTokenService -> JwtTokenService

6. Brands

Есть endpoint:

```http
POST /api/brands
```

Он создает бренд.

Важно:
- Бренд создает админ.
- Создатель бренда НЕ становится owner автоматически.
- Owner назначается отдельным endpoint.
- Поэтому CreateBrandCommand содержит только CreateBrandRequest, без UserId.

Текущий flow:
- BrandsController.Create
- CreateBrandCommand
- CreateBrandHandler
- IBrandRepository
- BrandRepository

7. Assign owner

Есть endpoint:

```http
POST /api/brands/{brandId}/owner
```

Body:

```json
{
  "userId": "USER_GUID"
}
```

Назначает owner'а бренду.

Flow:
- BrandsController.AssignOwner
- AssignBrandOwnerCommand
- AssignBrandOwnerHandler
- IBrandMembershipService
- BrandMembershipService в Application
- IBrandRepository / IUserRepository / IBrandMembershipRepository
- EF реализации репозиториев в Infrastructure

Правила:
- Проверить, что brand существует.
- Проверить, что user существует.
- Найти роль OWNER.
- Если у бренда уже есть OWNER и это другой user, вернуть ошибку:
  "Brand already has an owner"
- Если у бренда уже есть OWNER и это тот же user, запрос идемпотентен.
- Если membership user+brand уже есть, сменить роль на OWNER.
- Если membership нет, создать BrandMembership.

Пока DB-level unique constraint на owner не добавлен.
Это можно добавить позже для защиты от race condition.

8. Brand access / permissions

Есть IBrandAccessService в Application:

```csharp
Task<bool> CanAsync(
    Guid userId,
    Guid brandId,
    PermissionCode permission,
    CancellationToken cancellationToken);
```

Публичного GetUserRoleAsync нет. Это осознанно.
Внешний код не должен спрашивать "какая роль?", он должен спрашивать "может ли пользователь выполнить действие?".

BrandAccessService находится в Application.
Он использует IBrandMembershipRepository.
Hardcoded MVP mapping:

```csharp
OWNER => true

STAFF => StampIssue, BalanceView

CUSTOMER => BalanceView

unknown/no membership => false
```

STAFF НЕ имеет MetricManage.

Причина такого подхода:
- Сейчас роли статичны.
- Но контроллеры/handlers уже зависят от permission/action, а не от конкретной роли.
- В будущем можно заменить hardcoded mapping на таблицу role_permissions.

9. Текущий важный архитектурный вывод

Ранее часть бизнес-логики была в Infrastructure-сервисах:
- AuthService
- BrandAccessService
- BrandMembershipService

Это было исправлено.
Теперь:
- Application содержит бизнес-сценарии.
- Infrastructure содержит репозитории и техническую реализацию.

Не откатывать это обратно.

10. Что делать дальше

Следующий логичный этап:
- сделать endpoint создания метрики лояльности, доступный только пользователю с PermissionCode.MetricManage.
- Не писать EF в контроллере.
- Делать через Contracts DTO + Application command + handler + repository/service.
- В handler или Application service проверить:

```csharp
await brandAccessService.CanAsync(userId, brandId, PermissionCode.MetricManage, ct)
```

- Если false, вернуть access denied, controller должен вернуть 403.
- OWNER сможет создать метрику.
- STAFF должен получить 403.
- CUSTOMER должен получить 403.

11. Предпочтительный стиль реализации нового endpoint

Для Metrics:
- Contracts:
  - CreateMetricRequest
  - MetricResponse
- Application:
  - CreateMetricCommand
  - CreateMetricHandler
  - ILoyaltyMetricRepository или IMetricRepository
- Infrastructure:
  - LoyaltyMetricRepository / MetricRepository
- API:
  - MetricsController

Контроллер:
- [Authorize]
- достает userId из JWT ClaimTypes.NameIdentifier
- создает command
- вызывает handler
- маппит ошибки:
  - access denied -> Forbid / 403
  - validation/domain errors -> BadRequest
  - success -> Ok или Created

12. Проблемы/технический долг, помнить

- Telegram hash validation пока заглушка.
- Error mapping сейчас примитивный: BadRequest(result.Errors). Нужно позже нормализовать.
- Нужна DB-level гарантия "один OWNER на бренд", если появятся параллельные запросы.
- Русские комментарии в некоторых старых файлах могут отображаться mojibake в PowerShell. Не критично, но новые комментарии лучше писать ASCII или следить за UTF-8.
- API сейчас может ссылаться на Infrastructure, потому что startup/migrations/seeding используют AppDbContext. Это приемлемо для текущего этапа.
- Не добавлять сложную систему прав раньше времени.

13. Команды проверки

Сборка:

```powershell
dotnet build StampService.sln -p:OutputPath=c:\Programmer\StampService\StampService\obj\codex-build\
```

Docker postgres health:

```powershell
docker exec stamp_service_postgres pg_isready -U postgres -d stamp_service
```

EF update:

```powershell
cd C:\Programmer\StampService\StampService\src
dotnet ef database update --project .\StampService.Infrastructure\StampService.Infrastructure.csproj --startup-project .\StampService.API\StampService.API.csproj
```

14. Правило общения

- Отвечай по-русски.
- Делай изменения сам, если задача явно на реализацию.
- Перед изменениями сначала анализируй текущий код.
- После изменений всегда прогоняй build.
- Не предлагай архитектуру, противоречащую Clean Architecture/DDD.
- Если сомневаешься между быстрым MVP и чистым решением, объясни trade-off и выбери консервативно.
