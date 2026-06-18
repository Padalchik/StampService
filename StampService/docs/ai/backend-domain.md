# Backend/domain правила

Актуально на 2026-06-17. Ориентир - текущий код, не старые контекстные файлы.

## Архитектура

- `src/StampService.Domain` - сущности и инварианты.
- `src/StampService.Application` - use cases, commands/queries, порты репозиториев и сервисов.
- `src/StampService.Contracts` - DTO.
- `src/StampService.Infrastructure` - EF Core, PostgreSQL, репозитории, migrations, внешние сервисы.
- `src/StampService.API` - thin HTTP layer.
- `src/StampService.TelegramBot` - Telegram UX поверх Application.
- `src/StampService.Web` - React UI поверх typed API clients.

Правило: бизнес-логика остается в Domain/Application. API controllers, Telegram actions/screens и React components не должны становиться источником бизнес-правил.

## Identity и auth

- `User` больше не содержит `CustomerCode`; новые сценарии не проектировать вокруг `customerCode`.
- `UserIdentity` хранит внешние ключи. Активные identity - строки без `DeletedAt`.
- Полноценный пользовательский аккаунт создается штатно через успешный phone OTP или бизнес-операцию по телефону.
- `AuthService.VerifyPhoneCodeAsync` проверяет OTP по нормализованному телефону без `authCodeId`, затем `PhoneAccountService` находит/создает `User` с `Phone` identity и выдает JWT.
- `AuthService.LoginAsync(TelegramLoginRequest)` существует, но Telegram-login разрешен только для уже привязанной Telegram identity и пользователя с активной phone identity. Это не первичный web-вход.
- Штатная смена телефона: текущая JWT-сессия подтверждает `User.Id`, новый телефон подтверждается OTP, старая phone identity soft-delete, новая добавляется тому же пользователю.
- Штатной пользовательской перепривязки Telegram нет; перенос identity между пользователями запрещен.

SMS OTP:

- `RequestPhoneAuthCodeRequest.SendSms=false`: базовая доставка OTP администратору в Telegram.
- `SendSms=true`: дополнительно SMS клиенту через SmsAero, только если singleton `PhoneAuthSmsSettings` в БД включен.
- `SmsAero:SendAuthCodes` - стартовое/fallback значение при создании БД-настройки, не runtime source of truth.
- Credentials SmsAero должны быть в user-secrets/env, не в репозитории.

## Brand, роли, доступ

- `BrandMembership` описывает роль в бренде (`OWNER`, `STAFF`).
- `BrandCustomer` описывает клиентскую принадлежность пользователя к бренду (`BrandId + UserId`).
- Один `User` может быть сотрудником/владельцем и клиентом. Эти роли не заменяют друг друга.
- Системный админ определяется через admin options (`Admin:TelegramUserIds`/phone admin config), а не через brand role.
- Создание бренда с владельцем должно сохранять `Brand` и owner membership атомарно.
- Владелец должен быть добавлен и как `BrandCustomer` в сценариях создания/переназначения/demo, чтобы бренд был видим в клиентских сценариях.

## Brand customer и карточка клиента

- Поиск клиента в brand workspace обязан быть scoped по `BrandCustomer`. Нельзя открывать карточку только потому, что найден глобальный `User` с таким телефоном.
- Application query `GetBrandCustomerCardHandler` нормализует телефон, проверяет `BalanceView`, ищет через `IBrandCustomerRepository.GetCustomerByPhoneAsync(brandId, IdentityType.Phone, normalizedPhone)`, затем переиспользует wallet details.
- Важная граница: handler возвращает `UserErrors.RecipientNotFound()`, а API `BrandsController.GetCustomerCard` преобразует ровно эту ошибку в lookup DTO `found=false`.
- `POST /api/brands/{brandId}/customers/by-phone` явно создает `BrandCustomer` через `CreateBrandCustomerByPhoneCommand`; поиск карточки не создает клиента.

## Welcome rewards

- Настройки живут в `Brand`: `IsWelcomeRewardsEnabled`, `WelcomeMetricRewards`, `WelcomeCoinsAmount`, `WelcomeRewardComment`.
- При выключенных метриках нельзя сохранять welcome-штампы; при выключенных монетках нельзя сохранять welcome-монетки.
- Включенные welcome rewards не могут быть пустыми.
- Выдача welcome rewards происходит только при создании новой связки `BrandCustomer` для пары `BrandId + UserId` через `CreateBrandCustomerByPhoneCommand`.
- Глобальный phone-account может существовать заранее; для конкретного бренда клиент новый, пока нет `BrandCustomer`.
- Начисления идут через `IMetricLedgerService` / `ICoinLedgerService`, не прямой записью балансов.

## Ledger

- Штампы: `MetricBalance` + `StampTransaction`, операции через `MetricLedgerService`.
- Монетки: `CoinWallet` + `CoinTransaction`, операции через `CoinLedgerService`.
- Materialized value синхронизируется пересчетом из transaction ledger перед операцией.
- Конкурентность защищает `ILedgerOperationLock`; PostgreSQL host использует transaction-scoped advisory locks.
- Гранулярность lock: штампы `UserId + BrandId + MetricDefinitionId`, монетки `UserId + BrandId`.
- C# `lock` не подходит для консистентности между host instances.

## Wallet и reward digest

- Видимость бренда в пользовательском кошельке определяется `BrandCustomer`, а не наличием баланса или кошелька.
- `GetUserWalletOverviewHandler` строит список брендов из `IBrandCustomerRepository.GetUserBrandCustomersAsync`.
- Детали бренда в кошельке и карточке клиента переиспользуют один Application flow `GetUserWalletBrandDetails`.
- Reward digest и wallet-open сценарии находятся в `StampService.Application.CustomerNotifications`.

## Demo и уведомления

- `CreateUserDemoDataCommand` доступен только админу и owner выбранного бренда.
- Demo-fill требует существующего пользователя с phone identity; он не создает нового клиента из произвольного телефона.
- Demo-flow может создавать metrics/products и ledger-историю, но issue-операции клиенту должны вызывать `ICustomerNotificationService`.
- Не добавлять отдельный frontend/API endpoint "отправить уведомление" для demo; notification behavior меняется в Application handler или общем notification service.

## Telegram bot

- Bot живет в `src/StampService.TelegramBot` и использует локальную библиотеку `external/telegram-bot-flow`.
- Telegram onboarding phone-first: если Telegram identity еще не привязана, bot ведет через подтверждение телефона и привязывает Telegram к phone account.
- Stateful `authCodeId` допустим в profile/link/change flows, но первичный phone login web/Application проверяет актуальный OTP по телефону без session id.
- Telegram UX может использовать emoji, но web UI не должен копировать emoji в навигацию.
