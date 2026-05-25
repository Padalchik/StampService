# StampService: текущий операционный контекст

Актуально на 2026-05-25, Europe/Moscow.

Этот файл нужен для быстрого старта нового чата. Это не changelog и не детальная карта всех файлов. Здесь зафиксированы цель проекта, архитектурные границы, ключевые доменные решения и рабочие договоренности.

## Рабочие договоренности

- Общаться с пользователем на русском языке.
- Репозиторий: `C:\Programmer\StampService\StampService`.
- Перед изменениями читать существующий код и следовать локальным паттернам.
- Для поиска использовать `rg` / `rg --files`.
- Не запускать backend, Telegram bot, build, tests, migrations или dev servers без явного запроса пользователя. Если проверки не запускались, честно писать об этом.
- Ручные изменения файлов делать через `apply_patch`.
- Не откатывать чужие изменения в рабочем дереве без явного разрешения.
- Миграции EF не писать вручную: создавать через `dotnet ef migrations add`.
- Не превращать проект в микросервисы без отдельного архитектурного решения.

## Назначение проекта

StampService - loyalty-сервис для брендов. Основной интерфейс сейчас Telegram-бот, HTTP API и простой web UI используются как backend/API и тестовая/будущая пользовательская поверхность.

Ключевая идея: пользователь имеет один аккаунт (`User.Id`), к которому могут быть привязаны разные внешние идентификаторы/способы входа (`UserIdentity`): телефон, Telegram и потенциально другие. Все бизнес-данные, балансы, роли, бренды и история принадлежат `User.Id`, а не конкретному телефону или Telegram id.

Архитектурное правило авторизации: телефон является первичным внешним идентификатором клиента, а факт входа подтверждается OTP в момент авторизации. Наличие активной `Phone` identity само по себе не является отдельным доказательством текущего входа; OTP подтверждает право получить сессию по этому номеру. Telegram не является первичным аккаунтом и не должен создавать полноценный `User` сам по себе; Telegram identity привязывается к уже существующему или создаваемому через телефон аккаунту как дополнительный способ входа/связи.

Архитектурный стиль - modular monolith.

## Структура решения

- `src/StampService.Domain` - доменные сущности и инварианты.
- `src/StampService.Application` - use cases, commands/queries, интерфейсы репозиториев, application-сервисы.
- `src/StampService.Contracts` - DTO.
- `src/StampService.Infrastructure` - EF Core, PostgreSQL, репозитории, конфигурации, миграции, seeding.
- `src/StampService.API` - HTTP API.
- `src/StampService.TelegramBot` - Telegram UX: screens, endpoints, actions, navigation.
- `external/telegram-bot-flow` - локальная библиотека Telegram flow/navigation.
- `Tests` - тестовые проекты.

## Главные доменные области

Пользователи:

- `User` - аккаунт пользователя и владелец бизнес-данных.
- `UserIdentity` - внешний идентификатор/способ входа пользователя. `Phone` identity может быть создана как при OTP-входе, так и бизнес-сценарием начисления по телефону; вход все равно требует OTP.
- `IdentityType` сейчас включает Telegram и Phone.
- `RedemptionCode` - одноразовый код пользователя для операций сотрудника.

Бренды и роли:

- `Brand` - бренд.
- `BrandMembership` - членство пользователя в бренде.
- `Role` - роль в бренде (`OWNER`, `STAFF`).
- Системный администратор определяется через `Admin:TelegramUserIds`, а не через БД-роли.

Лояльность:

- Метрики и монеты - отдельные поддомены.
- Баланс не является первичным источником операции: изменения должны идти через ledger-сервисы и транзакции.
- Для метрик используются `MetricBalance` и `StampTransaction`.
- Для монет используются `CoinWallet` и `CoinTransaction`.
- Товары/награды за монеты представлены `CoinProduct`.

## Identity и авторизация

Текущая модель identity:

- Аккаунт пользователя стабилен по `User.Id`.
- Телефон является первичной `Phone` identity активного пользовательского аккаунта и основным внешним ключом клиента в loyalty-сценариях.
- Telegram является вторичной identity: способом входа/связи/уведомлений после привязки к телефонному аккаунту.
- Telegram и телефон не являются владельцами данных.
- Новые полноценные пользовательские аккаунты могут создаваться двумя штатными путями: после успешного OTP по телефону или при бизнес-операции начисления по телефону сотрудником. Во втором случае создается обычный `User` с активной `Phone` identity, а клиент позже получает доступ к этому аккаунту через OTP по тому же номеру.
- Telegram-only аккаунты не должны создаваться новыми сценариями. Если нужен переходный сценарий для старых данных, он должен быть явно выделен как legacy/migration flow.
- Штатной пользовательской перепривязки телефона или Telegram нет: если у пользователя уже есть активная identity этого типа, обычный сценарий профиля должен отказать, а не заменять её.
- `UserIdentity` не переносится между пользователями. Если внешний ключ уже принадлежит другому `User`, операция запрещается.
- Старые/исторические identity не перезаписываются и не удаляются прямыми пользовательскими сценариями. История сохраняется через soft delete только в явно спроектированных административных или migration-flow сценариях.
- Активные identity выбираются обычными запросами через global query filter `DeletedAt == null`.
- Историю identity можно смотреть отдельными админскими/audit-запросами через `IgnoreQueryFilters()`, если такая задача появится.

Важные правила:

- Нельзя создавать второй активный `Phone` identity для пользователя.
- Нельзя автоматически заменять активный `Phone` или `Telegram` identity у пользователя.
- Если телефон или Telegram уже привязан к другому пользователю, операция должна быть запрещена.
- Если identity уже активна у этого же пользователя, повторная привязка не должна запускать перепривязку или перенос данных.
- Нельзя создавать нового пользователя только по Telegram id.
- Нельзя использовать legacy Telegram-only аккаунт как источник для автоматического переноса Telegram identity на телефонный аккаунт.
- Нельзя отвязывать последний способ входа без отдельного бизнес-решения и UX-подтверждения.

## Идентификация клиента в операциях бренда

Для операций начисления метрик и монеток основной внешний идентификатор клиента - номер телефона. Сотрудник в web или Telegram вводит телефон клиента, backend/Application нормализует его и выполняет единый Application flow: найти пользователя по активной `Phone` identity или создать полноценного `User` с этой `Phone` identity, если клиента еще нет. После этого начисление проводится сразу на `User.Id` через ledger. Внутренним владельцем данных остается `User.Id`; телефон не становится primary key и не является владельцем балансов.

Auto-create клиента по телефону применяется только к начислениям. Списание метрик, ручное списание монеток и выдача товаров за монетки остаются по одноразовому `RedemptionCode`, потому что этот код подтверждает конкретную операцию клиента. Телефон используется для идентификации клиента при начислении, а не как подтверждение списания.

4-значный `CustomerCode` больше не используется в сценариях начисления метрик/монеток и в просмотре клиентских балансов. Старые HTTP/Application ветки начисления по `CustomerCode` удалены (`IssueCoinsCommand`, `IssueCoinsRequest`, `POST /api/brands/{brandId}/coins/issue`, `POST /api/metrics/{metricDefinitionId}/issue-by-customer-code`, `RecipientResolver`). Новые UI-сценарии не должны просить сотрудника вводить `CustomerCode`.

Ключевые Application use cases для нового flow: `IssueMetricByPhoneCommand` / `IssueMetricByPhoneHandler` и `IssueCoinsByPhoneCommand` / `IssueCoinsByPhoneHandler`. Web controllers и Telegram endpoints должны вызывать эти сценарии, а не делать предварительный resolve клиента по телефону в UI/API слое.

Просмотр балансов и истории клиента сотрудником также использует телефон как внешний идентификатор, но без auto-create: Application нормализует номер, ищет существующего пользователя по активной `Phone` identity и возвращает отказ, если клиент не найден. Это касается `GetBrandCustomerMetricBalancesQuery`, `GetCoinBalanceQuery` и `GetCoinHistoryQuery`. Auto-create по телефону остается только для начислений.

Управление сотрудниками бренда также переведено на phone-first модель. Добавление сотрудника выполняется через `AddBrandStaffByPhoneCommand`: Application нормализует номер телефона, ищет существующего пользователя по активной `Phone` identity и добавляет ему роль `STAFF` в бренде. Auto-create здесь не применяется: если телефонный пользователь не найден, сценарий должен отказать, потому что добавление сотрудника является управлением доступом, а не клиентским начислением. Telegram staff-flow больше не просит и не показывает `CustomerCode`; список, детали, подтверждение добавления и удаления сотрудника используют телефон как внешний идентификатор. Внутренние операции по-прежнему работают с `User.Id` и `BrandMembership`.

Админские операции назначения владельца бренда также больше не используют `CustomerCode`. `CreateBrandWithOwnerCommand` принимает телефон владельца, а `ReassignBrandOwnerCommand` - телефон нового владельца; Application нормализует номер и ищет существующий `User` по активной `Phone` identity. Auto-create не выполняется: создание бренда с владельцем и смена владельца являются управлением доступом, поэтому владелец должен уже иметь телефонный аккаунт. Telegram admin-flow ввода/подтверждения владельца показывает телефон; внутренним владельцем роли остается `User.Id` через `BrandMembership`.

## Телефонная авторизация и привязка

Реализованы два связанных сценария:

- вход по телефону через OTP;
- первичная привязка телефона в личном кабинете пользователя, если у аккаунта еще нет активной `Phone` identity.

OTP-коды:

- `PhoneAuthCode` хранит телефон, код, срок действия, использование и failed attempts.
- Код сейчас доставляется через `IPhoneAuthCodeSender`.
- Временная реализация отправляет код админу в Telegram через bot token и `Admin:TelegramUserIds`.
- Сервис SMS должен подключаться заменой реализации `IPhoneAuthCodeSender`, без переписывания use cases.
- Код подтверждения нормализуется перед проверкой.
- Первичный вход/регистрация по телефону подтверждает актуальный активный OTP для нормализованного номера телефона. Он не должен зависеть от технического состояния UI/session, кроме самого телефона и введенного кода.

Привязка телефона:

- `RequestPhoneLinkCodeHandler` создает OTP и возвращает `AuthCodeId`.
- `ConfirmPhoneLinkCodeHandler` подтверждает именно тот активный код, который был выдан текущему сценарию, если передан `AuthCodeId`.
- Telegram bot сохраняет `phoneNumber` и `authCodeId` в session data между вводом телефона и вводом кода. `AuthCodeId` нужен для stateful-сценариев уже авторизованного пользователя, например первичной привязки телефона в профиле.
- При успешной привязке бот показывает сообщение без лишней кнопки перехода в личный кабинет.
- В личном кабинете кнопка привязки телефона показывается только если у профиля нет активного телефона. Сценарий `Изменить телефон` удален из штатной UX-модели.

## Telegram UX

Telegram bot - основной рабочий UI.

Принципы:

- Первый вход в Telegram-боте должен приводить пользователя к телефонной авторизации/регистрации через OTP, если Telegram identity еще не привязана к аккаунту.
- После подтверждения актуального OTP по телефону бот может привязать текущий Telegram id к найденному или созданному телефонному `User`.
- Phone-first onboarding в Telegram является вариантом первичного телефонного входа: он не должен проверять код по `AuthCodeId` из session, чтобы техническое состояние Telegram-сценария не ломало подтверждение корректного актуального OTP для номера.
- Если `User` уже создан через web-вход по телефону, Telegram phone-first onboarding должен после успешного OTP найти этот `User` по активной `Phone` identity и добавить к нему новую `Telegram` identity. Новая `UserIdentity` должна сохраняться как `Added`/`INSERT`, а не как `Modified`/`UPDATE`; иначе EF получает `DbUpdateConcurrencyException` по несуществующей строке и пользователь видит ложную ошибку OTP.
- Если Telegram id уже принадлежит другому `User`, включая legacy Telegram-only аккаунт, phone-first onboarding должен отказать. Автоматического переноса Telegram identity между пользователями нет.
- Если у телефонного `User` уже есть активная Telegram identity, новая Telegram identity не добавляется и старая не заменяется.
- `EnsureTelegramUser`-подобные сценарии не должны создавать нового `User` только по Telegram id; они должны либо находить уже привязанную Telegram identity, либо переводить пользователя в phone-first onboarding.
- Не показывать пользователю технические поля, ids, internal codes и debug-информацию.
- Тексты должны быть пользовательскими и на русском.
- Для Telegram допустимы emoji в названиях основных кнопок и экранов, если они помогают навигации. Текущие основные labels: `🛍️ Мой кошелек` и `⚙️ Настройки аккаунта`.
- Повторяемые Telegram labels держать в одном локальном источнике внутри TelegramBot, а не размазывать строками по screens/endpoints/notifications.
- Вложенные сценарии должны иметь понятную кнопку назад/возврата, но не надо плодить лишние переходы.
- На главном меню не добавлять лишнюю кнопку `В главное меню`.
- Отключенные возможности бренда не должны протекать в UI.

Ключевые места:

- `src/StampService.TelegramBot/Features/Profile` - личный кабинет и первичная привязка телефона.
- `src/StampService.TelegramBot/Features/IssueMetric` - выдача метрик сотрудником; бот собирает телефон/количество и вызывает `IssueMetricByPhoneCommand`, без предварительного отказа при отсутствии клиента.
- `src/StampService.TelegramBot/Features/Coins` - начисление/списание монеток; начисление идет через `IssueCoinsByPhoneCommand` по телефону клиента, списание остается по одноразовому коду списания.
- `src/StampService.TelegramBot/Features/CustomerBalances` - просмотр балансов клиента; бот собирает телефон клиента, Application ищет существующую активную `Phone` identity и не создает нового пользователя.
- `src/StampService.TelegramBot/Features/Staff` - управление сотрудниками бренда; добавление сотрудника идет по телефону через `AddBrandStaffByPhoneCommand`, без auto-create и без `CustomerCode` в UI.
- `src/StampService.TelegramBot/Features/Admin` - системная админка; создание бренда с владельцем и смена владельца идут по телефону существующего пользователя через `CreateBrandWithOwnerCommand` и `ReassignBrandOwnerCommand`, без auto-create и без `CustomerCode` в UI.
- `src/StampService.TelegramBot/Common/Errors/BotErrorFormatter.cs` - перевод application errors в пользовательские сообщения.
- `external/telegram-bot-flow` - смотреть перед изменениями navigation/callback/input flow.

## HTTP API и web

API использует JWT и берет `UserId` из claims через `ApiControllerBase`.

Ключевые endpoints:

- auth через телефон;
- `GET /api/users/me`;
- request/confirm привязки телефона;
- confirm привязки Telegram.

HTTP auth через Telegram не должен создавать нового пользователя без подтвержденного телефона. Если Telegram-login endpoint сохраняется для совместимости, он должен работать только для уже привязанной Telegram identity или возвращать состояние, требующее телефонной авторизации.

Web/API сценарии профиля не должны перепривязывать телефон или Telegram поверх уже активной identity. Для смены телефона, перепривязки Telegram или миграции legacy identity нужен отдельный явно согласованный Application flow.

Web UI сейчас является вспомогательной и будущей поверхностью. При развитии полноценного web UI важно не дублировать бизнес-логику во frontend/backend controllers: логика должна оставаться в Application use cases.

Web и Telegram могут иметь разные UI labels для одного сценария: Telegram допускает emoji и более короткие кнопки, web должен оставаться спокойным рабочим интерфейсом без emoji в навигации. Повторяемые web labels держать в одном локальном frontend-источнике.

### Web brand workspace и операции с клиентами

В web добавлен первый рабочий сценарий для сотрудника/владельца бренда: раздел `Рабочие бренды` в React-приложении. Он концептуально повторяет Telegram-сценарии работы с клиентами, но остается web-native UI без Telegram session/navigation.

Ключевое правило: web не реализует бизнес-логику выдачи/списания сам. Он вызывает тонкие HTTP endpoints, а те используют существующие Application commands/queries и ledger-сервисы. Балансы метрик и монет нельзя менять напрямую из frontend или controllers.

Основные backend endpoints для web workspace:

- `GET /api/brands/mine` - список брендов текущего пользователя через `GetMyBrandsQuery`;
- `GET /api/brands/{brandId}/workspace` - рабочий контекст бренда, роли, permissions и feature toggles через `GetBrandWorkspaceQuery`;
- `GET /api/brands/{brandId}/metrics/issue-options` - активные метрики, доступные для выдачи, через `GetBrandIssueMetricsQuery`;
- `GET /api/brands/{brandId}/metrics/redeem-options?redemptionCode=...` - варианты списания метрик по коду списания через `GetRedeemMetricOptionsQuery`;
- `POST /api/metrics/{metricDefinitionId}/issue-by-phone` - основной web-friendly сценарий выдачи метрики по номеру телефона клиента через `IssueMetricByPhoneCommand`; Application находит или создает `User` по `Phone` identity и проводит ledger-начисление;
- `POST /api/metrics/{metricDefinitionId}/redeem` - списание метрики через существующий `RedeemMetricCommand`;
- `POST /api/brands/{brandId}/coins/issue-by-phone` - основной web-friendly сценарий начисления монеток по номеру телефона клиента через `IssueCoinsByPhoneCommand`; Application находит или создает `User` по `Phone` identity и проводит ledger-начисление;
- `POST /api/brands/{brandId}/coins/redeem` - ручное списание монеток через `RedeemCoinsCommand`;
- `GET /api/brands/{brandId}/coin-products/purchase-options?redemptionCode=...` и `POST /api/brands/{brandId}/coin-products/{productId}/purchase` - выдача товара за монетки через существующие CoinProduct use cases.

Ключевые frontend места:

- `src/StampService.Web/src/app/App.tsx` - раздел `Рабочие бренды` включен в основную навигацию;
- `src/StampService.Web/src/app/navigationLabels.ts` - повторяемые web labels навигации;
- `src/StampService.Web/src/brands/BrandWorkspacePage.tsx` - рабочий UI бренда и формы клиентских операций;
- `src/StampService.Web/src/brands/brandWorkspaceApi.ts` - typed API client для brand workspace;
- `src/StampService.Web/src/validation/phoneNumber.ts` - единая frontend-нормализация и маска телефона для web-полей;
- `src/StampService.Web/src/styles.css` - стили рабочего интерфейса.

Действия в web workspace скрываются по `CanIssue`, `CanRedeem` и feature toggles бренда: `IsMetricsEnabled`, `IsCoinsEnabled`, `IsCoinProductRedemptionEnabled`, `IsManualCoinRedemptionEnabled`. Backend всё равно остается авторитетным источником проверок доступа.

## Бренды, роли и доступы

- Бренд создается вместе с владельцем.
- Владелец бренда и системный администратор - разные концепции.
- Системный администратор задается конфигурацией Telegram id.
- Роли `OWNER` и `STAFF` сидятся инфраструктурой.
- Доступы к операциям бренда должны проверяться application-сценариями, а не только UI.

## Feature toggles бренда

У бренда есть независимые настройки:

- `IsMetricsEnabled`
- `IsCoinsEnabled`
- `IsCoinProductRedemptionEnabled`
- `IsManualCoinRedemptionEnabled`

UI и сценарии должны учитывать эти настройки. Если возможность выключена, пользователь не должен видеть соответствующие действия, а backend не должен выполнять запрещенную операцию.

## Ledger и консистентность

Для операций с балансами:

- не менять balances напрямую из UI/controller;
- создавать доменную операцию через Application;
- писать ledger transaction;
- синхронизировать материализованный баланс;
- сохранять понятный пользовательский комментарий, без технических префиксов.

Это особенно важно для метрик, монет, товаров и демо-данных.

## Reward Digest

Reward Digest - системное уведомление клиента о доступных наградах, не брендовая рассылка.

Основные компоненты:

- `RewardDigestSettings`
- `CustomerDigestState`
- `CustomerRewardDigestHostedService`
- `CustomerRewardDigestSender`

Outbox сейчас не вводился. Текущая модель - direct Telegram notifications.

## Демо и админские инструменты

Глобальная админка доступна только Telegram id из `Admin:TelegramUserIds`.

Демо-инструменты нужны для подготовки стенда и проверки UX. Они должны использовать доменные/Application-сценарии, а не обходить правила прямыми business insert/update.

Создание пользовательских демо-данных также переведено на phone-first модель. `CreateUserDemoDataCommand` принимает телефон пользователя, Application нормализует номер и ищет существующего `User` по активной `Phone` identity. Auto-create здесь не выполняется: демо-данные накатываются только на уже существующий телефонный аккаунт. Telegram admin demo-flow больше не просит `CustomerCode`; он собирает телефон, бренд и затем создает товары, метрики, балансы и историю через ledger/Application-сервисы.

Полный reset БД допустим только как явный админский/инфраструктурный сценарий с подтверждением.

## Инфраструктура

- Основная БД - PostgreSQL через EF Core.
- Soft delete реализован через `ISoftDelete` и global query filter.
- У `user_identities` уникальный индекс по активным identity: `deleted_at IS NULL`.
- Это позволяет хранить историю старых identity и иметь только одну активную привязку конкретного внешнего ключа.
- Конфиги Telegram bot token берутся из secrets/config; `Admin:TelegramUserIds` задается конфигурацией и уже содержит id пользователя `278225388`.

## Что не делать без явного запроса

- Не запускать процессы/билды/тесты без разрешения.
- Не переписывать архитектуру на микросервисы.
- Не вводить outbox без отдельного решения.
- Не менять модель системного администратора на БД-роль.
- Не показывать пользователю OTP-коды, debug, ids и служебные поля.
- Не делать прямые изменения балансов в обход ledger.
- Не удалять историю identity.
- Не реализовывать перепривязку телефона или Telegram как побочный эффект обычного профиля/onboarding.
- Не создавать новые Telegram-only аккаунты и не считать Telegram первичной регистрацией.
- Не отвязывать последний способ входа без отдельного бизнес-правила.

## Как подходить к новой задаче

1. Определить слой изменения: Domain, Application, Infrastructure, API, TelegramBot.
2. Найти аналогичный use case и повторить стиль.
3. Для бизнес-операций держать правила в Domain/Application, а controllers/bot endpoints использовать как thin UI layer.
4. Для Telegram UX проверить patterns в `external/telegram-bot-flow`.
5. Для identity помнить: `User.Id` - владелец данных, `Phone` identity обязательна для активного пользовательского аккаунта, остальные `UserIdentity` - дополнительные способы входа/связи.
6. Для любых изменений identity помнить: обычные сценарии только добавляют отсутствующую identity или отказывают при конфликте; перенос/замена identity допустимы только как отдельный согласованный flow.
7. В финальном ответе кратко указать изменения и явно сказать, какие проверки запускались.

## Логирование и диагностика

В проект подключены Serilog и Seq для структурированного логирования и диагностики проблем без попыток чинить поведение "наугад".

Архитектурное решение:

- Serilog подключается на уровне host-проектов `src/StampService.API` и `src/StampService.TelegramBot`, а не в `Infrastructure`.
- `Application` и `Infrastructure` используют стандартный `ILogger<T>` и не знают, куда физически пишутся логи.
- Host-проекты решают, какие sinks включены, как называется приложение в логах и какие lifecycle/request/update события логируются.
- Seq поднят как инфраструктурный сервис в `compose.yaml`.

Текущая конфигурация:

- `src/StampService.API/Program.cs` - bootstrap logger, Serilog provider, HTTP request logging, fatal startup/shutdown logging.
- `src/StampService.TelegramBot/Program.cs` - bootstrap logger, Serilog provider, logging для webhost части TelegramBot.
- `src/StampService.API/appsettings*.json` и `src/StampService.TelegramBot/appsettings*.json` - уровни логирования, sinks Console/Seq, свойство `Application`.
- `compose.yaml` - сервис `seq`, UI доступен локально на `http://localhost:5441`; first-run admin password сейчас `secure`.

В Seq логи разделяются по свойству `Application`: `StampService.API` и `StampService.TelegramBot`.

Что уже логируется:

- старт/остановка host-процессов;
- fatal-ошибки запуска;
- HTTP request logs API;
- ошибки из `ExceptionHandlingMiddleware`;
- существующие события `ILogger<T>` в Application/TelegramBot;
- EF Core SQL-команды в Development по настроенным уровням;
- TelegramBotFlow pipeline logs.

Что важно для следующих задач:

- Не логировать JWT, bot token, OTP, raw auth payloads и чувствительные персональные данные.
- Для бизнес-диагностики постепенно добавлять структурированные события в ключевые Application use cases: OTP, phone/Telegram linking, wallet open, issue/redeem metrics, issue/redeem coins, purchase coin product.
- В логах предпочитать стабильные идентификаторы и контекст операции: `UserId`, `BrandId`, `MetricDefinitionId`, `ProductId`, `TraceId`, результат операции и доменную причину отказа.

## Startup-уведомления TelegramBot

В `src/StampService.TelegramBot` добавлен `BotStartupNotificationHostedService`.

Назначение: после фактического старта TelegramBot отправить администраторам из `Admin:TelegramUserIds` два сообщения:

- ссылка на web UI;
- ссылка на Seq.

Настройки находятся в `StartupNotifications` в `src/StampService.TelegramBot/appsettings.json` и `appsettings.Development.json`: `Enabled`, `WebInterfaceUrl`, `SeqUrl`.

Это host-level диагностическая удобность для локального стенда. Она не должна попадать в Domain/Application и не должна использоваться как бизнес-уведомление пользователям.
