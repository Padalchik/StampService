# StampService: текущий операционный контекст

Актуально на 2026-06-03, Europe/Moscow.

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

Ключевая идея: пользователь имеет один аккаунт (`User.Id`), к которому могут быть привязаны разные внешние идентификаторы/способы входа (`UserIdentity`): телефон, Telegram и потенциально другие. Все бизнес-данные, балансы, роли, бренды и история принадлежат `User.Id`, а не конкретному телефону или Telegram id. Старый 4-значный `CustomerCode` больше не является частью доменной модели `User`; новые аккаунты создаются без генерации такого кода.

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
- `RedemptionCode` - одноразовый код списания для операций сотрудника.

`User` больше не содержит legacy-поле `CustomerCode`. Если в старых миграциях встречается `customer_code`, это исторический след эволюции схемы, а не актуальная модель. Новые сценарии должны опираться на `User.Id` внутри системы и на `Phone` identity как внешний клиентский идентификатор.

Бренды и роли:

- `Brand` - бренд.
- `BrandMembership` - членство пользователя в бренде.
- `Role` - роль в бренде (`OWNER`, `STAFF`).
- Системный администратор определяется через `Admin:TelegramUserIds`, а не через БД-роли.
- Создание бренда с владельцем должно быть атомарным: `Brand` и owner `BrandMembership` не должны сохраняться разными независимыми persistence-шагами. Текущий простой подход - сначала создать доменные объекты, затем поставить `Brand` и `BrandMembership` в один EF change tracker и выполнить один общий `SaveChanges` через repository `SaveAsync`. Не вводить отдельный transaction service для этого сценария без явной причины.

Лояльность:

- Метрики и монеты - отдельные поддомены.
- Баланс не является первичным источником операции: изменения должны идти через ledger-сервисы и транзакции.
- Для метрик используются `MetricBalance` и `StampTransaction`.
- Для монет используются `CoinWallet` и `CoinTransaction`.
- Товары/награды за монеты представлены `CoinProduct`.
- Materialized balance/wallet не является самостоятельным источником истины и должен оставаться консистентным с ledger-транзакциями. Для конкурентных ledger-операций используется Application-port `ILedgerOperationLock`; в PostgreSQL host он реализован через transaction-scoped advisory locks. Это защищает и существующие строки, и сценарии первого создания `MetricBalance`/`CoinWallet`, где row lock по отсутствующей строке не помог бы.
- Lock-гранулярность: для метрик ключом является `UserId + BrandId + MetricDefinitionId`, для монет - `UserId + BrandId`. C# `lock` для этого не подходит, потому что он защищает только один процесс, а консистентность нужна на уровне БД и всех host instances.

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

4-значный `CustomerCode` больше не используется в сценариях начисления метрик/монеток и в просмотре клиентских балансов. Старые HTTP/Application ветки начисления по `CustomerCode` удалены (`IssueCoinsCommand`, `IssueCoinsRequest`, `POST /api/brands/{brandId}/coins/issue`, `POST /api/metrics/{metricDefinitionId}/issue-by-customer-code`, `RecipientResolver`). Новые UI-сценарии не должны просить сотрудника вводить `CustomerCode`. На уровне Domain/Application удалена обязательность `CustomerCode` у `User`: `PhoneAccountService` создает телефонный аккаунт по display name и активной `Phone` identity, без `CustomerCodeGenerator`.

Публичные DTO операций с монетками также не должны возвращать `CustomerCode`. `CoinOperationResponse` используется API, web brand workspace, Telegram coin flows, Telegram coin-product purchase flow и customer notifications как результат ledger-операции; он содержит технические ids операции/кошелька/пользователя, имя клиента, тип операции, сумму, баланс и дату, но не старый клиентский код. Если UI нужно показать внешний идентификатор клиента, использовать телефонную identity в сценариях начисления или одноразовый `RedemptionCode` в сценариях списания, а не `CustomerCode`.

Ключевые Application use cases для нового flow: `IssueMetricByPhoneCommand` / `IssueMetricByPhoneHandler` и `IssueCoinsByPhoneCommand` / `IssueCoinsByPhoneHandler`. Web controllers и Telegram endpoints должны вызывать эти сценарии, а не делать предварительный resolve клиента по телефону в UI/API слое.

Уведомления клиенту о reward-операциях являются частью Application-сценариев, а не ответственностью конкретного входного канала. После успешного начисления `IssueMetricByPhoneHandler` и `IssueCoinsByPhoneHandler` вызывают `StampService.Application.CustomerNotifications.ICustomerNotificationService`. То же правило применено к списаниям: `RedeemMetricHandler`, `RedeemCoinsHandler` и `PurchaseCoinProductHandler` отправляют уведомление после успешного ledger-действия. Поэтому клиент получает Telegram-сообщение о начислении или списании независимо от того, пришла операция из Telegram-бота или из web/API.

Архитектурная раскладка уведомлений:

- `src/StampService.Application/CustomerNotifications/ICustomerNotificationService.cs` - порт Application-слоя для бизнес-уведомлений о начислении и списании;
- `src/StampService.Infrastructure/Services/TelegramCustomerNotificationService.cs` - инфраструктурная доставка для API/web через Telegram Bot API по активной `Telegram` identity клиента;
- `src/StampService.TelegramBot/Common/Notifications/CustomerNotificationApplicationAdapter.cs` - адаптер TelegramBot host, который сохраняет существующее session-aware поведение бота и не дает дублировать отправку в endpoint-ах;
- `src/StampService.Infrastructure/Services/CustomerAvailableRewardsFormatter.cs` - общий форматтер блока `Доступно сейчас` для notification-текстов; он показывает доступные клиенту награды текущего бренда по текущим балансам, а не только новые награды после последней операции;
- Telegram endpoints начисления/списания больше не должны отправлять уведомление вручную после handler-а, иначе появятся дубли.

Просмотр балансов и истории клиента сотрудником также использует телефон как внешний идентификатор, но без auto-create: Application нормализует номер, ищет существующего пользователя по активной `Phone` identity и возвращает отказ, если клиент не найден. Это касается `GetBrandCustomerMetricBalancesQuery`, `GetCoinBalanceQuery` и `GetCoinHistoryQuery`. Auto-create по телефону остается только для начислений.

Управление сотрудниками бренда также переведено на phone-first модель. Добавление сотрудника выполняется через `AddBrandStaffByPhoneCommand`: Application нормализует номер телефона, ищет существующего пользователя по активной `Phone` identity и добавляет ему роль `STAFF` в бренде. Auto-create здесь не применяется: если телефонный пользователь не найден, сценарий должен отказать, потому что добавление сотрудника является управлением доступом, а не клиентским начислением. Telegram staff-flow больше не просит и не показывает `CustomerCode`; список, детали, подтверждение добавления и удаления сотрудника используют телефон как внешний идентификатор. Внутренние операции по-прежнему работают с `User.Id` и `BrandMembership`.

Админские операции назначения владельца бренда также больше не используют `CustomerCode`. `CreateBrandWithOwnerCommand` принимает телефон владельца, а `ReassignBrandOwnerCommand` - телефон нового владельца; Application нормализует номер и ищет существующий `User` по активной `Phone` identity. Auto-create не выполняется: создание бренда с владельцем и смена владельца являются управлением доступом, поэтому владелец должен уже иметь телефонный аккаунт. Telegram admin-flow ввода/подтверждения владельца показывает телефон; внутренним владельцем роли остается `User.Id` через `BrandMembership`.

`CreateBrandWithOwnerHandler` должен создавать бренд и owner membership как один логический persistence unit. Репозиторий брендов имеет staging-метод `Add(Brand)` с `Result<Guid>` без немедленного сохранения; handler добавляет `Brand`, добавляет `BrandMembership` и выполняет один общий `SaveAsync`. Это сделано, чтобы не оставлять бренд без владельца при ошибке между двумя сохранениями. Старый `AddAsync` у `IBrandRepository` оставлен для сценариев, которым по-прежнему нужно "add and save" одним вызовом, например demo-flow.

Публичные пользовательские поверхности также переведены с `CustomerCode` на phone-first модель. Профиль и кошелек в Web/Telegram больше не возвращают и не показывают 4-значный код пользователя; профиль показывает имя и статусы identity, где телефон является основной клиентской identity. Кошелек использует одноразовый `RedemptionCode` для операций списания и overview брендов с балансами/доступными наградами. API по-прежнему может возвращать срок действия кода, но основной web-экран не делает его главным UI-элементом. `RedemptionCode` остается отдельным операционным подтверждением и не заменяет телефон как внешний идентификатор клиента.

Основной web-экран `Мой кошелёк` в `src/StampService.Web/src/wallet/WalletPage.tsx` переработан как быстрый client-facing mobile-first экран: сверху остается только заголовок раздела без описания, сразу под ним sticky-карточка кода списания с icon-only обновлением, ниже - секция `Мои бренды`. Главный список брендов не показывает историю, подробную детализацию или технические поля; он показывает название бренда, короткую meta-строку и до трех чипов конкретных доступных наград из `availableCoinProducts`/`availableMetrics` с учетом `isAvailable`, плюс чип `+N ещё`. Подробности бренда остаются отдельным экраном внутри кошелька через существующий `getBrandDetails` flow и кнопку `Открыть`. Backend/API/Application и `walletApi.ts` для этого UX не менялись.

Навигационно `Мой кошелёк` считается домашним экраном раздела кошелька. Если пользователь находится внутри детализации бренда и снова нажимает пункт `Мой кошелёк` в desktop/sidebar или mobile bottom navigation, web должен вернуться к основному списку брендов, даже если active section уже равен `wallet`. Это реализовано во frontend без изменения API: `src/StampService.Web/src/app/App.tsx` генерирует отдельный сигнал повторного перехода в кошелёк, а `WalletPage` по нему сбрасывает только внутренний state детализации бренда (`selectedBrandId`, details/error/loading), не перезагружая бизнес-данные кошелька без необходимости.

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
- `EnsureTelegramUser`-подобные сценарии не должны создавать нового `User` только по Telegram id; они должны либо находить уже привязанную Telegram identity, либо переводить пользователя в phone-first onboarding. Их response-модель не должна возвращать `CustomerCode`: Telegram actor определяется внутренним `User.Id` и display name, а не старым клиентским кодом.
- Не показывать пользователю технические поля, ids, internal codes и debug-информацию.
- Тексты должны быть пользовательскими и на русском.
- Для Telegram допустимы emoji в названиях основных кнопок и экранов, если они помогают навигации. Текущие основные labels: `🛍️ Мой кошелек` и `⚙️ Настройки аккаунта`.
- Повторяемые Telegram labels держать в одном локальном источнике внутри TelegramBot, а не размазывать строками по screens/endpoints/notifications.
- Вложенные сценарии должны иметь понятную кнопку назад/возврата, но не надо плодить лишние переходы.
- На главном меню не добавлять лишнюю кнопку `В главное меню`.
- Отключенные возможности бренда не должны протекать в UI.

Ключевые места:

- `src/StampService.TelegramBot/Features/Profile` - личный кабинет и первичная привязка телефона.
- `src/StampService.TelegramBot/Features/Wallet` - кошелек клиента; показывает код списания, балансы и награды, но не `CustomerCode`.
- `src/StampService.TelegramBot/Features/IssueMetric` - выдача метрик сотрудником; бот собирает телефон/количество и вызывает `IssueMetricByPhoneCommand`, без предварительного отказа при отсутствии клиента.
- `src/StampService.TelegramBot/Features/Coins` - начисление/списание монеток; начисление идет через `IssueCoinsByPhoneCommand` по телефону клиента, списание остается по одноразовому коду списания.
- `src/StampService.TelegramBot/Features/CoinProducts` - выдача товара за монетки; сотрудник вводит одноразовый `RedemptionCode`, выбирает доступный товар, Application списывает монетки через ledger, а финальный Telegram-экран показывает имя клиента, списание и баланс без `CustomerCode`.
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

В web развивается рабочая поверхность для сотрудника/владельца бренда: раздел `Рабочие бренды` в React-приложении. Он концептуально повторяет Telegram-сценарии работы с клиентами и owner-операции бренда, но остается web-native UI без Telegram session/navigation.

Ключевое правило: web не реализует бизнес-логику выдачи/списания, управления ролями или настроек сам. Он вызывает тонкие HTTP endpoints, а те используют существующие Application commands/queries и ledger-сервисы. Балансы метрик и монет нельзя менять напрямую из frontend или controllers.

Текущее покрытие web workspace:

- клиентские операции сотрудника/owner: выдача/списание метрик, начисление/списание монеток, выдача товара за монетки;
- управление метриками бренда: список, создание, редактирование;
- управление товарами за монетки: список, создание, редактирование, soft-delete/deactivate;
- управление сотрудниками: список, добавление по телефону через `AddBrandStaffByPhoneCommand`, удаление через `RemoveBrandStaffCommand`;
- настройки бренда: переключение `IsMetricsEnabled`, `IsCoinsEnabled`, `IsCoinProductRedemptionEnabled`, `IsManualCoinRedemptionEnabled` через `UpdateBrandRewardSettingsCommand`.

Web-управление сотрудниками следует phone-first модели. Пользовательский ввод - телефон сотрудника, auto-create не выполняется, внутренний `UserId` используется только как системный идентификатор уже выбранного/возвращенного сотрудника, а не как пользовательский ввод.

Основные backend endpoints для web workspace:

- `GET /api/brands/mine` - список брендов текущего пользователя через `GetMyBrandsQuery`;
- `GET /api/brands/{brandId}/workspace` - рабочий контекст бренда, роли, permissions и feature toggles через `GetBrandWorkspaceQuery`;
- `GET /api/brands/{brandId}/metrics`, `POST /api/brands/{brandId}/metrics`, `PUT /api/metrics/{metricDefinitionId}` - управление метриками через existing Application use cases;
- `GET /api/brands/{brandId}/metrics/issue-options` - активные метрики, доступные для выдачи, через `GetBrandIssueMetricsQuery`;
- `GET /api/brands/{brandId}/metrics/redeem-options?redemptionCode=...` - варианты списания метрик по коду списания через `GetRedeemMetricOptionsQuery`;
- `POST /api/metrics/{metricDefinitionId}/issue-by-phone` - основной web-friendly сценарий выдачи метрики по номеру телефона клиента через `IssueMetricByPhoneCommand`; Application находит или создает `User` по `Phone` identity и проводит ledger-начисление;
- `POST /api/metrics/{metricDefinitionId}/redeem` - списание метрики через существующий `RedeemMetricCommand`;
- `POST /api/brands/{brandId}/coins/issue-by-phone` - основной web-friendly сценарий начисления монеток по номеру телефона клиента через `IssueCoinsByPhoneCommand`; Application находит или создает `User` по `Phone` identity и проводит ledger-начисление;
- `POST /api/brands/{brandId}/coins/redeem` - ручное списание монеток через `RedeemCoinsCommand`;
- `GET /api/brands/{brandId}/coin-products/purchase-options?redemptionCode=...` и `POST /api/brands/{brandId}/coin-products/{productId}/purchase` - выдача товара за монетки через существующие CoinProduct use cases;
- `GET /api/brands/{brandId}/coin-products`, `POST /api/brands/{brandId}/coin-products`, `PUT /api/coin-products/{productId}`, `DELETE /api/coin-products/{productId}` - управление товарами за монетки;
- `GET /api/brands/{brandId}/staff`, `POST /api/brands/{brandId}/staff/by-phone`, `DELETE /api/brands/{brandId}/staff/{staffUserId}` - управление сотрудниками бренда;
- `PUT /api/brands/{brandId}/reward-settings` - управление feature toggles бренда.

Удаленные legacy endpoints не нужно поддерживать или восстанавливать без отдельного решения: старое добавление сотрудника без phone-first flow (`POST /api/brands/{brandId}/staff`) и старое начисление метрики без phone-first route (`POST /api/metrics/{metricDefinitionId}/issue`) удалены. Актуальные сценарии - `POST /api/brands/{brandId}/staff/by-phone` и `POST /api/metrics/{metricDefinitionId}/issue-by-phone`.

Ключевые frontend места:

- `src/StampService.Web/src/app/App.tsx` - раздел `Рабочие бренды` включен в основную навигацию;
- `src/StampService.Web/src/app/navigationLabels.ts` - повторяемые web labels навигации;
- `src/StampService.Web/src/brands/BrandWorkspacePage.tsx` - контейнер состояния выбранного бренда и загрузки workspace;
- `src/StampService.Web/src/brands/BrandSelector.tsx` - экран выбора рабочего бренда и загрузки `GET /api/brands/mine`, если список не передан из shell;
- `src/StampService.Web/src/brands/BrandWorkspace.tsx` - рабочая область выбранного бренда: клиентские операции, управление метриками, товарами, сотрудниками и настройками бренда;
- `src/StampService.Web/src/brands/brandWorkspaceApi.ts` - typed API client для brand workspace;
- `src/StampService.Web/src/validation/phoneNumber.ts` - единая frontend-нормализация и маска телефона для web-полей;
- `src/StampService.Web/src/styles.css` - стили рабочего интерфейса.

Действия в web workspace скрываются по permissions (`CanIssue`, `CanRedeem`, `CanViewBalances`, `CanManageMetrics`, `CanManageStaff`, `CanManageBrand`) и feature toggles бренда: `IsMetricsEnabled`, `IsCoinsEnabled`, `IsCoinProductRedemptionEnabled`, `IsManualCoinRedemptionEnabled`. Backend всё равно остается авторитетным источником проверок доступа.

Web-навигация следует тому же принципу, что Telegram UX: пользователь не должен видеть разделы, к которым у него нет прикладного доступа. `AppShell` загружает краткий контекст доступности через тонкие API (`GET /api/brands/mine` и `GET /api/admin/access`) и скрывает пункты меню без доступных брендов или без admin-доступа. Если у пользователя ровно один рабочий бренд, пункт меню называется `Работа` и сразу открывает workspace этого бренда; если брендов несколько, показывается `Рабочие бренды` со списком. Это UX-решение, а не security boundary: Application/API проверки остаются обязательными.

Текущая frontend-архитектура навигации: `AppShell` формирует единый список доступных `navigationItems` один раз из уже загруженного состояния брендов и admin access, а затем рендерит этот список разными layout-ами. Desktop/tablet использует левый `sidebar`, mobile использует fixed bottom navigation. Для mobile не создается отдельная role/access matrix; доступность пунктов остается общей. Повторяемые labels и короткие mobile labels хранятся в `src/StampService.Web/src/app/navigationLabels.ts`. `Админка` остается отдельным пунктом навигации, если доступна, и не прячется внутрь аккаунта.

Для права `CanViewBalances` в web добавлен тонкий сценарий просмотра балансов клиента по телефону через `GET /api/brands/{brandId}/customer-balances`, который вызывает существующий Application query `GetBrandCustomerMetricBalancesQuery`. Панель балансов показывается только при наличии `CanViewBalances` и включенных метрик или монеток.

### Web mobile layout

React Web UI адаптирован под мобильный формат на уровне общего stylesheet `src/StampService.Web/src/styles.css` и общего shell в `src/StampService.Web/src/app/App.tsx`. API, Application use cases, auth/session и бизнес-логика не менялись.

Ключевая идея адаптации: desktop-интерфейс остается рабочим dashboard/workspace, а на узких экранах те же сценарии складываются в одну колонку без горизонтального "съезда" элементов. Основные mobile rules находятся в media queries `max-width: 760px` и `max-width: 420px`.

Что важно для следующих изменений:

- базовый shell `app-shell` на desktop остается `sidebar + workspace`, на mobile становится одной колонкой;
- на `max-width: 760px` desktop/sidebar-навигация скрывается, а основная навигация становится fixed bottom navigation;
- bottom navigation всегда строится из общего списка доступных navigation items, поэтому бренды и админка не расходятся между desktop и mobile;
- mobile labels короткие: `Кошелёк`, `Работа`/`Бренды`, `Админка`, `Аккаунт`;
- workspace на mobile имеет нижний отступ под bottom navigation с учетом safe area, чтобы нижняя навигация не перекрывала карточки и кнопки;
- экран кошелька сохраняет sticky-карточку кода сверху, а список брендов скроллится между sticky-кодом и bottom navigation;
- карточки, формы, списки действий, brand workspace, wallet, profile и admin UI должны использовать существующие классы и паттерны из `styles.css`, а не новые локальные inline-стили;
- элементы с потенциально длинным текстом должны иметь `min-width: 0`, `overflow-wrap: anywhere` или mobile stacking, чтобы не выталкивать контейнер;
- кнопки в плотных mobile-блоках могут занимать всю ширину, чтобы текст не ломал layout;
- admin brand card требует специфичного mobile override `.brand-list-item.admin-brand-card`, потому что desktop-селектор тоже специфичный;
- проверка mobile layout сейчас была статической; build/dev server не запускались из-за рабочего ограничения не запускать проверки без явного разрешения.

В web-сессии явный выход находится в разделе аккаунта (`ProfilePage`) как действие `Выйти из аккаунта`. Общий header рабочего экрана не содержит logout-кнопку, чтобы на mobile не конкурировать с основной bottom navigation.

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
- `users.customer_code` удаляется миграцией `RemoveUserCustomerCode`; актуальный EF model snapshot больше не содержит `User.CustomerCode` и уникальный индекс по старому коду. Исторические migration files до этой миграции не редактировать вручную.
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

## Web-админка и admin access

В web добавлена глобальная админка как тонкий UI поверх HTTP API и существующих Application use cases. Она не является brand workspace и не должна смешиваться с обычными сценариями владельца/сотрудника бренда.

Ключевая архитектурная модель:

- системный администратор по-прежнему не является БД-ролью;
- для Telegram сохраняется проверка через `Admin:TelegramUserIds`;
- для web добавлена phone-first проверка через `Admin:PhoneNumbers`;
- текущий web-пользователь считается админом, если его `User.Id` из JWT имеет активную `Phone` identity из `Admin:PhoneNumbers`;
- текущий локальный admin phone: `+79214408362`;
- общий Application-представитель администратора - `AdminActor`, который может быть создан как `FromTelegram(telegramUserId)` или `FromUser(userId)`.

Основные файлы:

- `src/StampService.Application/Administration/AdminActor.cs`;
- `src/StampService.Application/Administration/AdminAccessService.cs`;
- `src/StampService.API/Controllers/AdminController.cs`;
- `src/StampService.Web/src/admin/AdminPage.tsx`;
- `src/StampService.Web/src/admin/adminApi.ts`.

Admin HTTP API:

- `GET /api/admin/access` - lightweight-проверка, показывать ли текущему web-пользователю глобальную админку;
- `GET /api/admin/brands` - список брендов для глобальной админки;
- `POST /api/admin/brands` - создать бренд и назначить владельца по телефону;
- `PUT /api/admin/brands/{brandId}/owner` - переназначить владельца бренда по телефону;
- `POST /api/admin/demo/brands` - создать демо-бренды;
- `POST /api/admin/demo/user-data` - создать демо-данные пользователю по телефону и бренду;
- `POST /api/admin/demo/reset` - очистить демонстрационную БД.

Важные ограничения:

- бизнес-правила админки остаются в Application; контроллеры только извлекают текущий `User.Id` и вызывают commands/queries;
- создание владельца/сотрудника не auto-create: телефон владельца должен принадлежать уже существующему phone-first пользователю;
- demo reset удаляет `users` и `user_identities`, поэтому текущий web JWT после reset становится невалидным; web после успешного reset очищает сессию и требует новый вход;
- reset БД остается опасной стендовой операцией и должен требовать явного подтверждения в UI;
- Reward Digest admin settings пока остаются Telegram-only, если отдельно не будет поставлена задача перенести их в web.

Текущая web-админка в `src/StampService.Web/src/admin/AdminPage.tsx` оформлена как mobile-first тонкий UI поверх этих же admin API. Она не меняет API-контракты и не переносит бизнес-логику во frontend. Верхний уровень экрана - вкладки `Бренды` и `Демо`: отдельной вкладки `Опасная зона` нет, очистка стендовой БД находится внутри `Демо` и требует текстового подтверждения `ОЧИСТИТЬ`. После успешного reset web очищает auth-сессию через `auth.signOut()`, потому что текущий JWT становится невалидным.

Вкладка `Бренды` использует локальный frontend-поиск по названию бренда, имени владельца и телефону владельца поверх результата `GET /api/admin/brands`. Список брендов сделан в стиле `BrandSelector`: компактные строки/плашки с avatar первой буквы бренда, названием, владельцем и телефоном владельца; technical ids, debug, `brandId`, `ownerUserId` и флаги настроек бренда пользователю не показываются. Создание бренда и смена владельца не являются inline-формами в списке: они открываются в bottom sheet и вызывают существующие `createBrandWithOwner` / `reassignBrandOwner`, затем перезагружают список брендов.

Вкладка `Демо` содержит карточки для `createDemoBrands`, `createUserDemoData` и `resetDemoDatabase`. Demo UI остается стендовым административным инструментом, визуально отделенным от пользовательских и брендовых рабочих сценариев, но использует тот же локальный стиль карточек, segmented controls и bottom sheet из `styles.css`.

## Web UI: статус дизайн-слоя

Экспериментальный параллельный HeroUI-слой удален. В `src/StampService.Web` больше нет маршрута `/login-heroui`, переключателей HeroUI-версий в профиле/кошельке, HeroUI-прототипов страниц, зависимостей `@heroui/*`, Tailwind-плагина и связанных `heroui-*` CSS-правил.

Удалены временные/legacy web surfaces, которые не являются частью целевого React-приложения: `/design-variants`, API redirects `/app` и `/phone-auth-test`, а также соответствующие static `wwwroot` HTML-прототипы. Новые web-задачи должны развивать `src/StampService.Web` и маршруты основного React app, а не восстанавливать эти страницы.

Актуальная архитектурная модель web UI:

- основные пользовательские экраны остаются штатными React/TypeScript-компонентами: `PhoneLoginPage`, `ProfilePage`, `WalletPage`, brand workspace и global admin;
- frontend остается thin UI поверх typed API clients и HTTP/Application сценариев, без переноса бизнес-правил в React;
- общий визуальный слой сейчас опирается на существующий `src/StampService.Web/src/styles.css` и локальные паттерны проекта;
- если в будущем будет выбрана UI-библиотека, ее нужно вводить через локальный app UI layer (`AppButton`, `AppCard`, `AppInput` и т.п.), а не через параллельные копии бизнес-экранов.

## Web UI: детализация бренда в кошельке

Экран детализации бренда внутри `Мой кошелёк` переработан как mobile-first клиентский экран поверх единого details-сценария.

Ключевое архитектурное решение:

- Web и Telegram по-прежнему используют общий Application query `GetUserWalletBrandDetailsQuery`;
- Web получает данные через тонкий endpoint `GET /api/wallet/brands/{brandId}/details`;
- `WalletPage.tsx` только рендерит presentation-friendly DTO и не вычисляет бизнес-правила наград;
- `walletApi.ts` остается typed API client без дополнительной логики;
- technical ids/debug/userId/brandId/actorUserId не выводятся пользователю.

Текущая структура web-экрана:

- `BrandDetailsScreen` остается отдельным экраном внутри `WalletPage`;
- верхняя строка содержит компактную кнопку `Назад`;
- название бренда и meta вынесены в отдельную hero-плашку;
- вкладки строятся динамически по реально возвращенным reward/history-секциям: `Метрики` идут первым пунктом, затем `Товары`, затем `История`;
- если у бренда выключен учет метрик или выдача товаров за монетки, соответствующая вкладка не показывается;
- segmented-control визуально адаптируется к фактическому числу вкладок и не оставляет пустые колонки под скрытые пункты;
- содержимое вкладки рендерится ниже без общей большой `surface-panel`;
- карточками являются сами товары/метрики/источники истории;
- `details.hintText` в web-детализации бренда не показывается.

Награды:

- вкладка `Товары` показывает секцию `CoinProducts`;
- вкладка `Метрики` показывает секцию `Metrics`;
- доступные элементы идут первыми, затем недоступные;
- на frontend допускается только UI-сортировка/раскрытие списка, без переноса бизнес-расчетов;
- `progressText` и `statusText` приходят из Application и используются как пользовательские presentation-тексты;
- доступные элементы показывают status `доступно`, недоступные - готовый текст вида `не хватает N`;
- progress bar показывается только когда `progressText` безопасно парсится как прогресс.

Application details-сценарий дополнен так, чтобы секция `Metrics` включала все активные метрики бренда, включая метрики без существующего `MetricBalance` у пользователя. Для таких метрик Application возвращает presentation item с текущим значением `0`, поэтому frontend просто рендерит готовый DTO.
Если reward feature выключена, details-сценарий не должен возвращать соответствующую reward section: `Metrics` скрывается через `IsMetricsEnabled`, `CoinProducts` - через связку `IsCoinsEnabled && IsCoinProductRedemptionEnabled`. Frontend в этом месте не дублирует бизнес-правила, а строит вкладки по пришедшим секциям.

История:

- вкладка `История` группирует операции по человекочитаемым источникам;
- для монеток используется имя `Монетки`;
- для товаров/метрик используется `sourceName`;
- детали выбранного источника открываются в bottom sheet/modal layer;
- операции показывают `amountText`, дату через `formatRuDateTime(createdAt)` и видимый комментарий, если он есть;
- `actorUserId`, raw `sourceType` и служебные значения в UI не выводятся.

Основные файлы:

- `src/StampService.Web/src/wallet/WalletPage.tsx`;
- `src/StampService.Web/src/styles.css`;
- `src/StampService.Application/Wallet/Queries/GetUserWalletBrandDetails/GetUserWalletBrandDetailsHandler.cs`;
- `Tests/StampService.ApplicationTests/Wallet/GetUserWalletBrandDetailsHandlerTests.cs`.

## Web UI: рабочая консоль бренда

Экран управления брендом в React web UI переработан из длинной страницы со всеми операционными и управленческими панелями в mobile-first рабочую консоль.

Ключевая UX-модель:

- для владельца верхний уровень разделяет `Операции` и `Управление`;
- для сотрудника верхний уровень не показывается, если ему доступны только рабочие операции;
- второй уровень выбирает предмет работы: в операциях это `Метрики`, `Товары`, `Монетки`, `Клиент`, в управлении - `Метрики`, `Товары`, `Сотрудники`, `Бренд`;
- третий уровень показывается только там, где у выбранного предмета больше одного действия, например `Выдать`/`Списать` для метрик и `Начислить`/`Списать` для монеток;
- если на уровне остается один доступный вариант, этот уровень не рендерится.

Архитектурные границы сохранены:

- backend, Application, API-контракты и бизнес-логика не менялись;
- существующие brand workspace HTTP-сценарии и typed API client используются как прежде;
- frontend только строит навигацию по permissions из `BrandWorkspaceResponse`, собирает ввод, вызывает существующие API-функции и показывает пользовательский результат;
- technical ids/debug/userId/brandId не выводятся пользователю;
- legacy `CustomerCode` не используется, операции работают через телефон клиента или одноразовый код списания.

Компонентная граница web workspace:

- `BrandWorkspacePage` больше не совмещает выбор бренда и рабочую область; это тонкий контейнер, который хранит выбранный `workspace`, вызывает `getBrandWorkspace(brandId)` и решает, какой экран показать;
- `BrandSelector` отвечает только за список рабочих брендов: использует `initialBrands` от `AppShell`, а если они не переданы - сам загружает `getMyBrands()`;
- `BrandWorkspace` отвечает только за рабочую область уже выбранного бренда и получает готовый `BrandWorkspaceResponse`;
- `initialBrandId` по-прежнему сразу открывает workspace, а `initialBrands` позволяют показать selector без повторной загрузки списка;
- возврат из workspace очищает выбранный workspace и возвращает пользователя к `BrandSelector`;
- ошибка загрузки workspace показывается рядом со списком брендов, чтобы пользователь не оставался на пустом экране.

Навигационно `Рабочие бренды` / `Работа` теперь тоже считается home/root action раздела, как `Мой кошелёк`. Повторное нажатие на пункт меню не является noop: `App.tsx` передает в `BrandWorkspacePage` отдельный сигнал повторного перехода. При нескольких брендах контейнер сбрасывает выбранный workspace и возвращает пользователя к `BrandSelector`; при одном бренде заново открывает рабочую форму этого бренда и сбрасывает внутренние вкладки/формы `BrandWorkspace`.

Текущая структура UI:

- верх экрана содержит кнопку `Назад` и отдельную белую плашку бренда с названием и человекочитаемой ролью;
- flags/chips настроек бренда в плашке не показываются;
- рабочая зона показывает только один активный сценарий, а не набор всех panels подряд;
- формы, options и результаты являются отдельными карточками;
- management-панели метрик, товаров, сотрудников и настроек бренда переиспользуют прежнюю логику, но открываются только внутри выбранного раздела консоли;
- для списания метрик и выдачи товаров за монетки frontend сортирует уже полученные options так, чтобы доступные к списанию/выдаче награды шли первыми, затем недоступные с причиной нехватки.

Основные файлы:

- `src/StampService.Web/src/brands/BrandWorkspacePage.tsx`;
- `src/StampService.Web/src/brands/BrandSelector.tsx`;
- `src/StampService.Web/src/brands/BrandWorkspace.tsx`;
- `src/StampService.Web/src/brands/brandWorkspaceApi.ts`;
- `src/StampService.Web/src/styles.css`.
