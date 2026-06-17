# StampService: текущий операционный контекст

Актуально на 2026-06-17, Europe/Moscow.

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
- При запуске тестов через debugger учитывать first-chance exceptions: `RedemptionCodeGeneratorTests.GenerateAsync_WhenAllCombinationsAreReserved_ShouldThrow` ожидаемо бросает `InvalidOperationException` внутри `Assert.ThrowsAsync`. Это не падение теста, если обычный `dotnet test` проходит.

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
- `RedemptionCode` - одноразовый код списания для операций сотрудника. Штатный срок жизни нового кода сейчас 10 минут и задается в `CreateRedemptionCodeHandler`; это продуктовая настройка Application-сценария генерации, а не инвариант доменной сущности.

`User` больше не содержит legacy-поле `CustomerCode`. Если в старых миграциях встречается `customer_code`, это исторический след эволюции схемы, а не актуальная модель. Новые сценарии должны опираться на `User.Id` внутри системы и на `Phone` identity как внешний клиентский идентификатор.

Бренды и роли:

- `Brand` - бренд.
- `BrandMembership` - членство пользователя в бренде.
- `BrandCustomer` - связка пользователя с конкретным брендом как клиента (`BrandId + UserId`). Это отдельная модель от `BrandMembership`: сотрудник/владелец описывается membership/role, а клиентская принадлежность к бренду описывается `BrandCustomer`. Один и тот же `User` может быть клиентом нескольких брендов, но поиск клиента внутри бренда всегда должен быть scoped этой связкой.
- `Role` - роль в бренде (`OWNER`, `STAFF`).
- Системный администратор определяется через `Admin:TelegramUserIds`, а не через БД-роли.
- Создание бренда с владельцем должно быть атомарным: `Brand` и owner `BrandMembership` не должны сохраняться разными независимыми persistence-шагами. Текущий простой подход - сначала создать доменные объекты, затем поставить `Brand` и `BrandMembership` в один EF change tracker и выполнить один общий `SaveChanges` через repository `SaveAsync`. Не вводить отдельный transaction service для этого сценария без явной причины.
- Владелец бренда должен добавляться и в `BrandCustomer`: при создании бренда с владельцем, переназначении владельца и создании demo brands нужно гарантировать связку owner `UserId` с брендом как клиента. Это нужно, чтобы owner был видим в клиентских сценариях своего бренда и чтобы модель `BrandCustomer` была полной с первого дня жизни бренда.

Лояльность:

- Метрики и монеты - отдельные поддомены.
- Продуктовый термин для метрик в React Web UI - `Штампы`. Внутренние доменные/API-имена остаются `Metric*`, `metrics`, `Metrics`, `MetricBalance`, потому что это текущий контракт Application/API и схема кода. Новые web-тексты должны показывать пользователю `Штампы`/`штампы`, не переименовывая технический слой без отдельного архитектурного решения.
- Баланс не является первичным источником операции: изменения должны идти через ledger-сервисы и транзакции.
- Для метрик используются `MetricBalance` и `StampTransaction`.
- Для монет используются `CoinWallet` и `CoinTransaction`.
- Товары/награды за монеты представлены `CoinProduct`.
- Видимость бренда в кошельке пользователя определяется `BrandCustomer`, а не наличием `MetricBalance` или `CoinWallet`. Если пользователь является клиентом бренда, карточка бренда должна показываться в `Мой кошелёк` даже при нулевых балансах и без доступных наград. Балансы, кошельки и ledger-история только наполняют карточку остатками, доступными наградами и прогрессом; они не являются критерием показа бренда. Это важно для явного создания клиента из brand workspace: новая связка `BrandCustomer` сразу делает бренд видимым клиенту, а welcome rewards остаются отдельным необязательным начислением.
- Materialized balance/wallet не является самостоятельным источником истины и должен оставаться консистентным с ledger-транзакциями. Для конкурентных ledger-операций используется Application-port `ILedgerOperationLock`; в PostgreSQL host он реализован через transaction-scoped advisory locks. Это защищает и существующие строки, и сценарии первого создания `MetricBalance`/`CoinWallet`, где row lock по отсутствующей строке не помог бы.
- Lock-гранулярность: для метрик ключом является `UserId + BrandId + MetricDefinitionId`, для монет - `UserId + BrandId`. C# `lock` для этого не подходит, потому что он защищает только один процесс, а консистентность нужна на уровне БД и всех host instances.
- У бренда есть настройки приветственных наград: общий флаг включения, набор метрик с количеством по каждой метрике, сумма монеток и комментарий для истории. Доменные настройки учитывают feature toggles бренда: при выключенных метриках нельзя сохранить welcome-метрики, при выключенных монетках нельзя сохранить welcome-монетки; отключение глобального feature toggle очищает несовместимую часть welcome-настроек.
- Приветственные награды выдаются только в явном сценарии создания клиента из draft-карточки brand workspace (`CreateBrandCustomerByPhoneCommand`) и только когда создается новая связка `BrandCustomer` для пары `BrandId + UserId`. Глобальный phone-account может уже существовать в системе, но для этого бренда клиент считается новым, пока нет `BrandCustomer`. Начисление идет через `IMetricLedgerService` и `ICoinLedgerService`, а не прямой записью балансов; комментарий по умолчанию - `Приветственная награда`.
- Persistence welcome-настроек: scalar-поля хранятся на `brands`, а метрики с количеством - отдельными строками `brand_welcome_metric_rewards` (`brand_id`, `metric_definition_id`, `amount`). Старый промежуточный формат списка metric ids мигрирован в строки с `amount = 1`.

## Identity и авторизация

Текущая модель identity:

- Аккаунт пользователя стабилен по `User.Id`.
- Телефон является первичной `Phone` identity активного пользовательского аккаунта и основным внешним ключом клиента в loyalty-сценариях.
- Telegram является вторичной identity: способом входа/связи/уведомлений после привязки к телефонному аккаунту.
- Telegram и телефон не являются владельцами данных.
- Новые полноценные пользовательские аккаунты могут создаваться двумя штатными путями: после успешного OTP по телефону или при бизнес-операции начисления по телефону сотрудником. Во втором случае создается обычный `User` с активной `Phone` identity, а клиент позже получает доступ к этому аккаунту через OTP по тому же номеру.
- Telegram-only аккаунты не должны создаваться новыми сценариями. Если нужен переходный сценарий для старых данных, он должен быть явно выделен как legacy/migration flow.
- Штатная пользовательская смена телефона реализована отдельным profile-flow: текущая JWT-сессия подтверждает владение текущим аккаунтом, новый номер подтверждается OTP, старая `Phone` identity soft-delete, новая `Phone` identity добавляется к тому же `User.Id`. Штатной пользовательской перепривязки Telegram нет: если у пользователя уже есть активная Telegram identity, обычный сценарий профиля должен отказать, а не заменять её.
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

Важное архитектурное разделение: глобальный `User` и клиент конкретного бренда - разные понятия. `User` остается единым аккаунтом человека с identity, балансами и историей по `User.Id`. Клиентом бренда пользователь становится только после появления связки `BrandCustomer` (`BrandId + UserId`). Поэтому поиск клиента в brand workspace, просмотр балансов/истории и операции по redemption code должны проверять не только существование активной `Phone` identity или `User`, но и принадлежность пользователя конкретному бренду через `BrandCustomer`.

`BrandCustomer` живет в домене брендов (`StampService.Domain.Brand.BrandCustomer`) и хранится в таблице `brand_customers`. На уровне БД для активных строк действует уникальность пары `brand_id + user_id` с учетом soft delete. Создание/проверка связки вынесены в Application service `IBrandCustomerService` / `BrandCustomerService`, а чтение - в `IBrandCustomerRepository`. Не надо размазывать ad hoc-проверки `brandId + userId` по handlers, если можно использовать этот сервис/репозиторий.

Для операций начисления метрик и монеток основной внешний идентификатор клиента - номер телефона. Сотрудник в web или Telegram вводит телефон клиента, backend/Application нормализует его и ищет существующего пользователя по активной `Phone` identity. Начисления не создают глобальный phone-account неявно; если пользователя с активным телефоном нет, это бизнес-отказ `RecipientNotFound`. Если пользователь найден, handler гарантирует наличие `BrandCustomer` для текущего бренда и затем проводит начисление на `User.Id` через ledger. Внутренним владельцем данных остается `User.Id`; телефон не становится primary key и не является владельцем балансов.

Явное создание нового phone-клиента для brand workspace вынесено в отдельный сценарий `CreateBrandCustomerByPhoneCommand` и HTTP endpoint `POST /api/brands/{brandId}/customers/by-phone`. Этот сценарий может найти существующий глобальный phone-account или создать новый `User` с активной `Phone` identity, затем создать `BrandCustomer` для выбранного бренда. Именно этот draft-flow отвечает за welcome rewards при первой связке пользователя с брендом. Поиск карточки клиента остается read-only и не создает ни `User`, ни `BrandCustomer`.

Списание метрик, ручное списание монеток и выдача товаров за монетки остаются по одноразовому `RedemptionCode`, потому что этот код подтверждает конкретную операцию клиента. После нахождения пользователя по активному коду Application должен дополнительно проверить, что пользователь является `BrandCustomer` текущего бренда. Redemption code не должен позволять сотруднику бренда списывать награды глобального пользователя, который не является клиентом этого бренда.

4-значный `CustomerCode` больше не используется в сценариях начисления метрик/монеток и в просмотре клиентских балансов. Старые HTTP/Application ветки начисления по `CustomerCode` удалены (`IssueCoinsCommand`, `IssueCoinsRequest`, `POST /api/brands/{brandId}/coins/issue`, `POST /api/metrics/{metricDefinitionId}/issue-by-customer-code`, `RecipientResolver`). Новые UI-сценарии не должны просить сотрудника вводить `CustomerCode`. На уровне Domain/Application удалена обязательность `CustomerCode` у `User`: `PhoneAccountService` создает телефонный аккаунт по display name и активной `Phone` identity, без `CustomerCodeGenerator`.

Публичные DTO операций с монетками также не должны возвращать `CustomerCode`. `CoinOperationResponse` используется API, web brand workspace, Telegram coin flows, Telegram coin-product purchase flow и customer notifications как результат ledger-операции; он содержит технические ids операции/кошелька/пользователя, имя клиента, тип операции, сумму, баланс и дату, но не старый клиентский код. Если UI нужно показать внешний идентификатор клиента, использовать телефонную identity в сценариях начисления или одноразовый `RedemptionCode` в сценариях списания, а не `CustomerCode`.

Ключевые Application use cases для операций начисления: `IssueMetricByPhoneCommand` / `IssueMetricByPhoneHandler` и `IssueCoinsByPhoneCommand` / `IssueCoinsByPhoneHandler`. Они работают только с уже существующей активной `Phone` identity и через `BrandCustomerService.EnsureAsync` добавляют пользователя в клиенты бренда при первой successful business operation в этом бренде.

Уведомления клиенту о reward-операциях являются частью Application-сценариев, а не ответственностью конкретного входного канала. После успешного начисления `IssueMetricByPhoneHandler` и `IssueCoinsByPhoneHandler` вызывают `StampService.Application.CustomerNotifications.ICustomerNotificationService`. То же правило применено к списаниям: `RedeemMetricHandler`, `RedeemCoinsHandler` и `PurchaseCoinProductHandler` отправляют уведомление после успешного ledger-действия. Поэтому клиент получает Telegram-сообщение о начислении или списании независимо от того, пришла операция из Telegram-бота или из web/API.

Архитектурная раскладка уведомлений:

- `src/StampService.Application/CustomerNotifications/ICustomerNotificationService.cs` - порт Application-слоя для бизнес-уведомлений о начислении и списании;
- `src/StampService.Infrastructure/Services/TelegramCustomerNotificationService.cs` - инфраструктурная доставка для API/web через Telegram Bot API по активной `Telegram` identity клиента;
- `src/StampService.TelegramBot/Common/Notifications/CustomerNotificationApplicationAdapter.cs` - адаптер TelegramBot host, который сохраняет существующее session-aware поведение бота и не дает дублировать отправку в endpoint-ах;
- `src/StampService.Infrastructure/Services/CustomerAvailableRewardsFormatter.cs` - общий форматтер блока `Доступно сейчас` для notification-текстов; он показывает доступные клиенту награды текущего бренда по текущим балансам, а не только новые награды после последней операции;
- Telegram endpoints начисления/списания больше не должны отправлять уведомление вручную после handler-а, иначе появятся дубли.

Просмотр балансов и истории клиента сотрудником также использует телефон как внешний идентификатор, но без auto-create: Application нормализует номер и ищет пользователя только среди `BrandCustomer` текущего бренда. Глобальный `User` с таким телефоном, который является клиентом другого бренда, не должен находиться. Это касается `GetBrandCustomerCardQuery`, `GetBrandCustomerMetricBalancesQuery`, `GetCoinBalanceQuery` и `GetCoinHistoryQuery`.

Управление сотрудниками бренда также переведено на phone-first модель. Добавление сотрудника выполняется через `AddBrandStaffByPhoneCommand`: Application нормализует номер телефона, ищет существующего пользователя по активной `Phone` identity и добавляет ему роль `STAFF` в бренде. Auto-create здесь не применяется: если телефонный пользователь не найден, сценарий должен отказать, потому что добавление сотрудника является управлением доступом, а не клиентским начислением. Telegram staff-flow больше не просит и не показывает `CustomerCode`; список, детали, подтверждение добавления и удаления сотрудника используют телефон как внешний идентификатор. Внутренние операции по-прежнему работают с `User.Id` и `BrandMembership`.

Админские операции назначения владельца бренда также больше не используют `CustomerCode`. `CreateBrandWithOwnerCommand` принимает телефон владельца, а `ReassignBrandOwnerCommand` - телефон нового владельца; Application нормализует номер и ищет существующий `User` по активной `Phone` identity. Auto-create не выполняется: создание бренда с владельцем и смена владельца являются управлением доступом, поэтому владелец должен уже иметь телефонный аккаунт. Telegram admin-flow ввода/подтверждения владельца показывает телефон; внутренним владельцем роли остается `User.Id` через `BrandMembership`.

`CreateBrandWithOwnerHandler` должен создавать бренд и owner membership как один логический persistence unit. Репозиторий брендов имеет staging-метод `Add(Brand)` с `Result<Guid>` без немедленного сохранения; handler добавляет `Brand`, добавляет `BrandMembership` и выполняет один общий `SaveAsync`. Это сделано, чтобы не оставлять бренд без владельца при ошибке между двумя сохранениями.

Общая договоренность по repository write style: staging methods (`Add`, `Remove`, domain mutators над tracked entity) не должны сами вызывать `SaveChanges`; save boundary задает Application handler/service через явный `SaveAsync`. Старый `IBrandRepository.AddAsync` удален, demo-brand creation тоже переведен на staged `Add` для бренда, owner membership, метрик и товаров с одним `SaveAsync` в конце. Если нужен метод, который одновременно создает и сохраняет, его следует вводить как явно named use-case/repository operation, а не как обычный `AddAsync`.

Тестовое покрытие этой договоренности обновлено: fake repositories в ApplicationTests имеют `SaveCount`, `CreateBrandWithOwner` фиксирует отсутствие отдельного save у `BrandRepository`, а `CreateDemoBrandsHandlerTests` фиксирует staged-создание demo brands с одним общим save boundary. Это не EF integration test, а Application-level защита от возврата к смешанному `AddAsync`/`SaveAsync` стилю.

Публичные пользовательские поверхности также переведены с `CustomerCode` на phone-first модель. Профиль и кошелек в Web/Telegram больше не возвращают и не показывают 4-значный код пользователя; профиль показывает имя и статусы identity, где телефон является основной клиентской identity. Кошелек использует одноразовый `RedemptionCode` для операций списания и overview наград по брендам с балансами/доступными наградами. API по-прежнему возвращает срок действия кода, а новый код живет 10 минут, но основной web-экран не делает срок главным UI-элементом. `RedemptionCode` остается отдельным операционным подтверждением и не заменяет телефон как внешний идентификатор клиента.

В Web UI код списания должен оставаться актуальным, пока у пользователя открыт кошелек. `RedemptionCode` одноразовый: после успешного списания старый код используется, а следующий запрос текущего кода через Application-сценарий `CreateRedemptionCodeHandler` вернет существующий активный код или создаст новый. Для web это реализовано без отдельного realtime-транспорта: `WalletPage` фоново обновляет только карточку кода через typed API client `getCurrentRedemptionCode`, который вызывает тонкий endpoint `POST /api/users/me/redemption-code`; ручное обновление передает `forceRefreshCode=true`, а фоновое обновление делает обычное чтение/создание актуального кода. `OpenUserWallet` остается сценарием открытия кошелька и полной загрузки overview, а не polling endpoint.

Основной web-экран `Мой кошелёк` в `src/StampService.Web/src/wallet/WalletPage.tsx` переработан как быстрый client-facing mobile-first экран: сверху остается только заголовок раздела без описания, сразу под ним sticky-карточка кода списания с icon-only обновлением, ниже - секция `Мои награды`. Эта секция концептуально показывает награды пользователя, сгруппированные по брендам, а не управление брендами: счетчик брендов в заголовке не выводится, а пустое состояние формулируется как отсутствие наград. Главный список не показывает историю, подробную детализацию или технические поля; он показывает название бренда, короткую meta-строку и до трех чипов конкретных доступных наград из `availableCoinProducts`/`availableMetrics` с учетом `isAvailable`, плюс чип `+N ещё`. Подробности бренда открываются через отдельный экран `BrandWalletPage` по существующему `getBrandDetails` flow кликом по всей карточке. Бизнес-логика генерации/использования кода остается в Application; frontend только обновляет отображаемый актуальный код.

Навигационно `Мой кошелёк` считается домашним экраном раздела кошелька. Если пользователь находится внутри детализации бренда и снова нажимает пункт `Мой кошелёк` в desktop/sidebar или mobile bottom navigation, web должен вернуться к основному списку брендов, даже если active section уже равен `wallet`. Это реализовано во frontend без изменения API: `src/StampService.Web/src/app/App.tsx` генерирует отдельный сигнал повторного перехода в кошелёк, а `WalletPage` по нему сбрасывает только внутренний state детализации бренда (`selectedBrandId`, details/error/loading), не перезагружая бизнес-данные кошелька без необходимости.

Компонентная граница кошелька сейчас разделена так: `WalletPage` отвечает за загрузку кошелька, актуализацию кода списания, список брендов и переход в бренд; `BrandWalletPage` отвечает за экран наград выбранного бренда; `RedemptionCodeCard` является общей компактной карточкой одноразового кода списания; `WalletBrandDetailsBlock` является общим блоком вкладок `Штампы` / `Монетки` / `История` и переиспользуется как в кошельке, так и в карточке клиента brand workspace. Карточка кода в `BrandWalletPage` расположена после плашки бренда и до блока наград.

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
- В личном кабинете первичная привязка телефона показывается, если у профиля нет активного телефона. Если активный телефон уже есть, показывается отдельный сценарий `Изменить телефон`, который подтверждает новый номер через OTP и заменяет активную `Phone` identity через soft delete старой.

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
- `src/StampService.TelegramBot/Features/IssueMetric` - выдача метрик сотрудником; бот собирает телефон/количество и вызывает `IssueMetricByPhoneCommand`. Команда ожидает, что клиент уже создан как phone-user; если активной `Phone` identity нет, это отказ бизнес-сценария, а не повод создавать клиента неявно.
- `src/StampService.TelegramBot/Features/Coins` - начисление/списание монеток; начисление идет через `IssueCoinsByPhoneCommand` по телефону уже существующего клиента, списание остается по одноразовому коду списания.
- `src/StampService.TelegramBot/Features/CoinProducts` - выдача товара за монетки; сотрудник вводит одноразовый `RedemptionCode`, выбирает доступный товар, Application списывает монетки через ledger, а финальный Telegram-экран показывает имя клиента, списание и баланс без `CustomerCode`.
- `src/StampService.TelegramBot/Features/CustomerBalances` - просмотр балансов клиента; бот собирает телефон клиента, Application ищет `BrandCustomer` текущего бренда по активной `Phone` identity и не создает нового пользователя или связь с брендом.
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

Web/API сценарии профиля не должны перепривязывать Telegram поверх уже активной identity. Смена телефона разрешена только через отдельный явно согласованный Application flow `RequestPhoneChangeCode` / `ConfirmPhoneChangeCode`: новый номер подтверждается OTP, identity другого `User` не переносится, все бизнес-данные остаются на прежнем `User.Id`.

Web UI сейчас является вспомогательной и будущей поверхностью. При развитии полноценного web UI важно не дублировать бизнес-логику во frontend/backend controllers: логика должна оставаться в Application use cases.

Web и Telegram могут иметь разные UI labels для одного сценария: Telegram допускает emoji и более короткие кнопки, web должен оставаться спокойным рабочим интерфейсом без emoji в навигации. Повторяемые web labels держать в одном локальном frontend-источнике.

### Web brand workspace и операции с клиентами

В web развивается рабочая поверхность для сотрудника/владельца бренда: раздел `Рабочие бренды` в React-приложении. Он концептуально повторяет Telegram-сценарии работы с клиентами и owner-операции бренда, но остается web-native UI без Telegram session/navigation.

Ключевое правило: web не реализует бизнес-логику выдачи/списания, управления ролями или настроек сам. Он вызывает тонкие HTTP endpoints, а те используют существующие Application commands/queries и ledger-сервисы. Балансы метрик и монет нельзя менять напрямую из frontend или controllers.

Текущее покрытие web workspace:

- клиентские операции сотрудника/owner: выдача/списание штампов (технически метрик), начисление/списание монеток, выдача товара за монетки;
- управление штампами бренда: список, создание, редактирование поверх существующих `Metric*` use cases;
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
- `POST /api/metrics/{metricDefinitionId}/issue-by-phone` - основной web-friendly сценарий выдачи метрики по номеру телефона клиента через `IssueMetricByPhoneCommand`; Application находит существующего `User` по активной `Phone` identity и проводит ledger-начисление, но не создает клиента неявно;
- `POST /api/metrics/{metricDefinitionId}/redeem` - списание метрики через существующий `RedeemMetricCommand`;
- `POST /api/brands/{brandId}/coins/issue-by-phone` - основной web-friendly сценарий начисления монеток по номеру телефона клиента через `IssueCoinsByPhoneCommand`; Application находит существующего `User` по активной `Phone` identity и проводит ledger-начисление, но не создает клиента неявно;
- `POST /api/brands/{brandId}/customers/by-phone` - явное создание phone-клиента для brand workspace через `CreateBrandCustomerByPhoneCommand`; после создания frontend должен заново загрузить полноценную карточку через `GET /customer-card`;
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
- `src/StampService.Web/src/brands/BrandWorkspace.tsx` - рабочая область выбранного бренда: клиентские операции, управление штампами, товарами, сотрудниками и настройками бренда;
- `src/StampService.Web/src/brands/brandWorkspaceApi.ts` - typed API client для brand workspace;
- `src/StampService.Web/src/validation/phoneNumber.ts` - единая frontend-нормализация и маска телефона для web-полей;
- `src/StampService.Web/src/styles.css` - стили рабочего интерфейса.

Действия в web workspace скрываются по permissions (`CanIssue`, `CanRedeem`, `CanViewBalances`, `CanManageMetrics`, `CanManageStaff`, `CanManageBrand`) и feature toggles бренда: `IsMetricsEnabled`, `IsCoinsEnabled`, `IsCoinProductRedemptionEnabled`, `IsManualCoinRedemptionEnabled`. Backend всё равно остается авторитетным источником проверок доступа.

Web-навигация следует тому же принципу, что Telegram UX: пользователь не должен видеть разделы, к которым у него нет прикладного доступа. `AppShell` загружает краткий контекст доступности через тонкие API (`GET /api/brands/mine` и `GET /api/admin/access`) и скрывает пункты меню без доступных брендов или без admin-доступа. Если у пользователя ровно один рабочий бренд, пункт меню называется `Работа` и сразу открывает workspace этого бренда; если брендов несколько, показывается `Рабочие бренды` со списком. Это UX-решение, а не security boundary: Application/API проверки остаются обязательными.

Текущая frontend-архитектура навигации: `AppShell` формирует единый список доступных `navigationItems` один раз из уже загруженного состояния брендов и admin access, а затем рендерит этот список разными layout-ами. Desktop/tablet использует левый `sidebar`, mobile использует fixed bottom navigation. Для mobile не создается отдельная role/access matrix; доступность пунктов остается общей. Повторяемые labels и короткие mobile labels хранятся в `src/StampService.Web/src/app/navigationLabels.ts`. `Админка` остается отдельным пунктом навигации, если доступна, и не прячется внутрь аккаунта.

Для права `CanViewBalances` в web добавлен тонкий сценарий просмотра балансов клиента по телефону через `GET /api/brands/{brandId}/customer-balances`, который вызывает существующий Application query `GetBrandCustomerMetricBalancesQuery`. Query должен искать клиента через `BrandCustomer` текущего бренда, а не глобально по `UserIdentity`. Панель балансов показывается только при наличии `CanViewBalances` и включенных метрик или монеток.

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
- segmented controls в brand workspace должны оставаться читаемыми на mobile: короткие подписи вкладок вроде `Сотрудники` не должны переноситься на две строки; для этого допустимы меньшие отступы/шрифт и flex-распределение ширины вместо равных жестких колонок;
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

`CreateDemoBrandsCommand` создает демо-бренды для текущего администратора без отдельного persistence step на каждый бренд: Application выбирает до 5 отсутствующих шаблонов, создает `Brand`, owner `BrandMembership`, метрики и товары как staged entities и сохраняет их одним `SaveAsync`. Это стендовый admin-flow, но он следует тем же save-boundary правилам, что и обычное создание бренда с владельцем.

Полный reset БД допустим только как явный админский/инфраструктурный сценарий с подтверждением.

## Инфраструктура

- Основная БД - PostgreSQL через EF Core.
- Soft delete реализован через `ISoftDelete` и global query filter.
- У `user_identities` уникальный индекс по активным identity: `deleted_at IS NULL`.
- Это позволяет хранить историю старых identity и иметь только одну активную привязку конкретного внешнего ключа.
- Все штатные lookup-сценарии по внешней identity должны работать только с активными identity. `IUserRepository.GetByIdentityAsync` концептуально означает поиск по `DeletedAt IS NULL`; soft-deleted phone/Telegram identity является историей, а не способом входа, начисления, назначения сотрудника/владельца или фильтрации audit.
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
- Не реализовывать перепривязку Telegram или смену телефона как побочный эффект onboarding. Смена телефона допускается только отдельным profile-flow с OTP нового номера.
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
- Технические логи остаются в Serilog/Seq. Они нужны для exceptions, недоступной инфраструктуры, ошибок БД/API, request/update pipeline и разборов по `TraceId`/`CorrelationId`.
- Бизнес-диагностика вынесена отдельно в `BusinessAuditLog`, а не смешивается с техническими логами.
- В логах и audit-событиях предпочитать стабильные идентификаторы и контекст операции: `UserId`, `BrandId`, `MetricDefinitionId`, `ProductId`, `TraceId`, результат операции и доменную причину отказа.

## BusinessAuditLog

Для бизнес-разборов добавлен отдельный структурированный аудит ключевых операций. Это не замена Serilog/Seq: Seq отвечает на вопрос "что технически сломалось", а `BusinessAuditLog` отвечает на вопрос владельца бизнеса "кто, когда и что сделал с клиентом/брендом, чем это закончилось и почему".

Архитектурная модель:

- доменная сущность аудита находится в `src/StampService.Domain/Audit`;
- Application слой содержит audit abstractions/read models и пишет события из use cases, а не из frontend/controllers;
- Infrastructure сохраняет audit records в PostgreSQL через EF и предоставляет read repository для админского журнала;
- API/TelegramBot дают audit-контекст канала и текущего actor-а: web/API канал `Web`, TelegramBot канал `Telegram`;
- API request logging обогащается `TraceId`, `CorrelationId`, `UserId`, `BrandId`; `X-Correlation-ID` можно использовать для связи request logs и audit record.

Что сейчас покрыто audit-событиями:

- начисление и списание монеток;
- выдача товара за монетки;
- выдача и списание метрик;
- добавление сотрудника по телефону;
- изменение reward-настроек бренда.

Статусы аудита:

- `Succeeded` - бизнес-операция успешно завершена;
- `Rejected` - ожидаемый бизнес-отказ, например недостаточно баланса, клиент не найден, feature выключен;
- `Failed` зарезервирован для технически сорванных операций, но настоящие exceptions по-прежнему в первую очередь разбираются через Serilog/Seq.

Чувствительные данные в audit не пишутся: JWT, OTP, redemption code, raw phone/auth payloads и секреты не должны попадать ни в `MetadataJson`, ни в комментарии системного происхождения. Комментарий операции считается пользовательским бизнес-комментарием и должен показываться аккуратно.

Audit sink сделан fail-safe для уже завершенной бизнес-операции: если запись audit-события не сохранилась после успешного use case, это логируется как техническая ошибка в Serilog/Seq, но не откатывает выданные монетки/метрики. При этом audit persistence изолирован от основного request `DbContext`: `BusinessAuditSink` создает отдельный `AppDbContext` через `IDbContextFactory<AppDbContext>` и сохраняет только audit record. Это принципиально, потому что audit не должен случайно коммитить pending изменения бизнес-use-case, например использованный redemption code, баланс, membership или другие изменения, которые еще не прошли свою persistence boundary. Миграция для таблицы audit создана как `20260603124027_AddBusinessAuditLogs`; применение миграции к конкретной БД остается отдельным инфраструктурным шагом.

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
- `GET /api/admin/audit-logs` - журнал бизнес-операций из `BusinessAuditLog` для разбора действий по брендам, клиентам и сотрудникам;
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

Текущая web-админка в `src/StampService.Web/src/admin/AdminPage.tsx` оформлена как mobile-first тонкий UI поверх этих же admin API. Она не меняет API-контракты и не переносит бизнес-логику во frontend. Верхний уровень экрана - вкладки `Бренды`, `Журнал` и `Демо`: отдельной вкладки `Опасная зона` нет, очистка стендовой БД находится внутри `Демо` и требует текстового подтверждения `ОЧИСТИТЬ`. После успешного reset web очищает auth-сессию через `auth.signOut()`, потому что текущий JWT становится невалидным.

Вкладка `Бренды` использует локальный frontend-поиск по названию бренда, имени владельца и телефону владельца поверх результата `GET /api/admin/brands`. Список брендов сделан в стиле `BrandSelector`: компактные строки/плашки с avatar первой буквы бренда, названием, владельцем и телефоном владельца; technical ids, debug, `brandId`, `ownerUserId` и флаги настроек бренда пользователю не показываются. Создание бренда и смена владельца не являются inline-формами в списке: они открываются в bottom sheet и вызывают существующие `createBrandWithOwner` / `reassignBrandOwner`, затем перезагружают список брендов.

Вкладка `Журнал` показывает бизнес-аудит из `BusinessAuditLog` через `GET /api/admin/audit-logs`. Фильтры: период, бренд, клиент по телефону, исполнитель, тип операции, статус и лимит записей. Данные обновляются автоматически при изменении фильтров, текстовые поля используют debounce, а телефон отправляется только когда введен полный валидный номер. UI показывает человекочитаемые summary/status/reason/comment и не выводит raw ids, trace ids, redemption codes или debug metadata. Если нужен технический разбор, связь с Seq выполняется по `TraceId` на backend/audit уровне, а не через показ служебных полей пользователю.

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
- `WalletPage.tsx` остается контейнером основного кошелька и не вычисляет бизнес-правила наград;
- `BrandWalletPage.tsx` отвечает за экран наград выбранного бренда поверх presentation-friendly DTO;
- `walletApi.ts` остается typed API client без дополнительной логики;
- technical ids/debug/userId/brandId/actorUserId не выводятся пользователю.

Текущая структура web-экрана:

- `BrandWalletPage` является отдельным экраном наград бренда, а не вложенным компонентом внутри `WalletPage`;
- `RedemptionCodeCard` переиспользуется для плашки одноразового кода списания в основном кошельке и в экране наград бренда;
- `WalletBrandDetailsBlock` вынесен в отдельный модуль и используется и в кошельке, и в brand workspace;
- верхняя строка содержит компактную кнопку `Назад`;
- название бренда и meta вынесены в отдельную hero-плашку;
- вкладки строятся динамически по details DTO: пользовательская вкладка `Штампы` идет первым пунктом, затем `Монетки`, затем `История`;
- `Штампы` показываются только если у бренда включены метрики; `Монетки` показываются только если у бренда включены монетки; `История` остается общей вкладкой;
- segmented-control визуально адаптируется к фактическому числу вкладок и не оставляет пустые колонки под скрытые пункты;
- содержимое вкладки рендерится ниже без общей большой `surface-panel`;
- карточками являются сами товары/штампы/источники истории;
- `details.hintText` в web-детализации бренда не показывается.

Награды:

- вкладка `Монетки` показывает текущий баланс монеток клиента в бренде;
- товары/награды за монетки не являются отдельной вкладкой кошелька: если выдача товаров за монетки включена и Application вернул reward section `CoinProducts`, она рендерится ниже баланса внутри `Монеток`;
- если выдача товаров за монетки выключена, вкладка `Монетки` все равно показывает баланс без блока товаров;
- вкладка `Штампы` показывает техническую секцию `Metrics`;
- порядок наград в `Штампах` и товарах внутри `Монеток`: сначала полностью доступные, затем недоступные с начатым накоплением, затем недоступные без прогресса;
- на frontend допускается только UI-сортировка/раскрытие списка, без переноса бизнес-расчетов стоимости, требований и нехватки;
- `progressText` и `statusText` приходят из Application и используются как пользовательские presentation-тексты;
- доступные элементы показывают status `доступно`, недоступные - готовый текст вида `не хватает N`;
- progress bar показывается только когда `progressText` безопасно парсится как прогресс.

Application details-сценарий возвращает web-ready данные для навигации детализации бренда: feature flags `IsMetricsEnabled`, `IsCoinsEnabled`, `IsCoinProductRedemptionEnabled` и `CoinBalance` находятся в `UserWalletBrandDetailsResponse`, чтобы frontend не выводил доступность разделов из наличия reward sections. Секции `RewardSections` по-прежнему описывают конкретные блоки наград, а не сами продуктовые разделы.
Секция `Metrics` включает все активные метрики бренда, включая метрики без существующего `MetricBalance` у пользователя. Для таких метрик Application возвращает presentation item с текущим значением `0`, поэтому frontend просто рендерит готовый DTO и называет эту секцию для пользователя `Штампы`.
Если reward feature выключена, details-сценарий не должен возвращать соответствующую reward section: `Metrics` скрывается через `IsMetricsEnabled`, `CoinProducts` - через связку `IsCoinsEnabled && IsCoinProductRedemptionEnabled`. Frontend в этом месте не дублирует бизнес-правила: продуктовые вкладки строятся по флагам details DTO, а конкретные блоки наград - по пришедшим секциям.

История:

- вкладка `История` группирует операции по человекочитаемым источникам;
- для монеток используется имя `Монетки`;
- для товаров/штампов используется `sourceName`;
- детали выбранного источника открываются в bottom sheet/modal layer;
- операции показывают `amountText`, дату через `formatRuDateTime(createdAt)` и видимый комментарий, если он есть;
- `actorUserId`, raw `sourceType` и служебные значения в UI не выводятся.

Основные файлы:

- `src/StampService.Web/src/wallet/WalletPage.tsx`;
- `src/StampService.Web/src/wallet/BrandWalletPage.tsx`;
- `src/StampService.Web/src/wallet/RedemptionCodeCard.tsx`;
- `src/StampService.Web/src/wallet/WalletBrandDetailsBlock.tsx`;
- `src/StampService.Web/src/styles.css`;
- `src/StampService.Application/Wallet/Queries/GetUserWalletBrandDetails/GetUserWalletBrandDetailsHandler.cs`;
- `Tests/StampService.ApplicationTests/Wallet/GetUserWalletBrandDetailsHandlerTests.cs`.

## Web UI: рабочая консоль бренда

Экран управления брендом в React web UI переработан из длинной страницы со всеми операционными и управленческими панелями в mobile-first рабочую консоль. Актуальная UX-модель рабочей области строится вокруг выбранного клиента: сотрудник сначала открывает карточку клиента по телефону, затем выполняет операции в контексте этой карточки.

Ключевая UX-модель:

- если клиент не выбран, рабочая зона показывает отдельный экран поиска клиента по телефону;
- поиск клиента является read-only сценарием: frontend валидирует/нормализует телефон как остальные phone-поля, а Application ищет пользователя только среди `BrandCustomer` текущего бренда по активной `Phone` identity;
- поиск/открытие карточки не создает пользователя автоматически; начисления по телефону также не создают клиента неявно и требуют уже существующую активную `Phone` identity;
- экран поиска хранит локальную историю последних успешно открытых телефонов в `localStorage` с привязкой к `brandId`; это best-effort UX-кэш только для быстрого повторного открытия карточки, а не источник истины и не бизнес-аудит;
- таблица `Недавние номера` хранит до 6 номеров, может быть очищена пользователем кнопкой `Очистить`, и пополняется при успешном открытии существующей карточки или после явного создания нового клиента, когда frontend повторно загрузил полноценную карточку;
- если клиент найден, рабочая зона показывает обычную карточку клиента и операции над выбранным клиентом;
- если клиент не найден, frontend не создает пользователя и не остается на тупиковой ошибке: `GET /api/brands/{brandId}/customer-card` возвращает успешный lookup-ответ `found=false`, `BrandCustomerSearchScreen` сообщает в `BrandWorkspace` нормализованный телефон, а `BrandWorkspace` открывает локальную draft-карточку `Новый клиент` для этого телефона;
- draft-карточка является только UX-состоянием web-клиента: в ней нет `User.Id`, `CustomerCode`, реальных балансов, истории и `WalletBrandDetailsBlock`; она показывает телефон и поясняет, что начисления и другие операции станут доступны только после явного создания клиента;
- для draft-клиента блок `Операции` не рендерится. В карточке доступно действие явного создания клиента; если `BrandWorkspaceResponse.WelcomeRewards` сообщает, что приветственные награды включены и в них есть доступные rewards, кнопка показывает, что вместе с созданием будут выданы приветственные награды;
- приветственные награды являются backend/Application-поведением сценария создания `BrandCustomer` из draft-карточки. Frontend не вызывает отдельные issue endpoints для welcome-наград, не моделирует ledger-операции сам и после успешного `createBrandCustomerByPhone` всегда перезагружает полноценную карточку через `getBrandCustomerCard`;
- обычная карточка клиента показывает имя, телефон и блок `Штампы` / `Монетки` / `История`;
- блок наград/истории реально переиспользуется из wallet UI через общий компонент `WalletBrandDetailsBlock` из `src/StampService.Web/src/wallet/WalletBrandDetailsBlock.tsx`, поэтому изменения в визуальном паттерне наград применяются и в кошельке, и в карточке клиента;
- операции переиспользуют существующие панели `BrandWorkspace`: выдача штампов/монеток берет телефон из выбранной карточки, а списание штампов, списание монеток и выдача товара по-прежнему требуют одноразовый код списания клиента;
- при явном создании из draft-карточки frontend вызывает `createBrandCustomerByPhone`, затем обязательно повторно загружает карточку через `getBrandCustomerCard` по телефону, заменяет draft на обычную карточку и только после этого показывает обычные операции; issue-by-phone endpoints не используются для создания клиента;
- управление брендом вынесено из основной рабочей зоны в отдельный экран настроек, который открывается кнопкой-шестерёнкой в плашке бренда;
- внутри настроек доступны управленческие разделы `Штампы`, `Товары`, `Сотрудники`, `Бренд` по permissions. В настройках бренда владелец может включить/выключить welcome rewards, выбрать несколько активных штампов с количеством начисления по каждому, задать сумму монеток и комментарий истории; UI показывает и валидирует эти controls с учетом permissions и feature toggles из `BrandWorkspaceResponse`, но backend остается источником enforcement;
- операционная выдача товаров за монетки находится внутри раздела `Монетки`; отдельного операционного раздела `Товары` нет;
- третий уровень показывается только там, где у выбранного предмета больше одного действия, например `Выдать`/`Списать` для штампов и `Начислить`/`Выдать товар`/`Списать вручную` для монеток;
- если на уровне остается один доступный вариант, этот уровень не рендерится.

Архитектурные границы сохранены:

- для открытия карточки клиента добавлен thin HTTP endpoint `GET /api/brands/{brandId}/customer-card` поверх Application query `GetBrandCustomerCardQuery`;
- HTTP endpoint возвращает lookup DTO (`found` + nullable `card`): существующий клиент приходит как `200 OK, found=true`, а ожидаемый сценарий отсутствующего клиента - как `200 OK, found=false`, чтобы frontend не трактовал draft-flow как сетевую ошибку; реальные ошибки доступа, валидации и прочие отказы остаются error-response через общий `EndpointResult`;
- Application query нормализует телефон, проверяет доступ к просмотру балансов, ищет клиента через `IBrandCustomerRepository.GetCustomerByPhoneAsync(brandId, Phone, normalizedPhone)` и переиспользует wallet details-сценарий для данных карточки. Нельзя возвращаться к глобальному `IUserRepository.GetByIdentityAsync` в этом flow, иначе пользователь-клиент одного бренда начнет находиться в другом бренде;
- бизнес-логика поиска клиента не реализуется во frontend;
- существующие brand workspace HTTP-сценарии и typed API client используются для операций и управления как прежде;
- frontend только строит навигацию по permissions из `BrandWorkspaceResponse`, собирает ввод, вызывает существующие API-функции, показывает пользовательский результат и может хранить локальные UX-подсказки вроде недавних телефонов;
- technical ids/debug/userId/brandId не выводятся пользователю;
- legacy `CustomerCode` не используется, операции работают через телефон клиента или одноразовый код списания.

Компонентная граница web workspace:

- `BrandWorkspacePage` больше не совмещает выбор бренда и рабочую область; это тонкий контейнер, который хранит выбранный `workspace`, вызывает `getBrandWorkspace(brandId)` и решает, какой экран показать;
- `BrandSelector` отвечает только за список рабочих брендов: использует `initialBrands` от `AppShell`, а если они не переданы - сам загружает `getMyBrands()`;
- `BrandWorkspace` отвечает за рабочую область уже выбранного бренда и переключает внутренние экраны выбранного бренда: поиск клиента, работа с найденным клиентом и настройки бренда;
- `BrandCustomerSearchScreen` отвечает за экран поиска клиента: ввод телефона, вызов `getBrandCustomerCard`, разделение not found и прочих ошибок, а также таблицу `Недавние номера` из локального `localStorage`; not found передается наверх как запрос открыть локальную карточку нового клиента, а не как создание клиента;
- `SelectedCustomerWorkspace` внутри `BrandWorkspace` отвечает за экран работы с выбранным клиентом: для существующего клиента показывает карточку и операции, для локального нового клиента показывает только карточку создания без операций;
- `initialBrandId` по-прежнему сразу открывает workspace, а `initialBrands` позволяют показать selector без повторной загрузки списка;
- возврат из workspace очищает выбранный workspace и возвращает пользователя к `BrandSelector`;
- ошибка загрузки workspace показывается рядом со списком брендов, чтобы пользователь не оставался на пустом экране.

Навигационно `Рабочие бренды` / `Работа` теперь тоже считается home/root action раздела, как `Мой кошелёк`. Повторное нажатие на пункт меню не является noop: `App.tsx` передает в `BrandWorkspacePage` отдельный сигнал повторного перехода. При нескольких брендах контейнер сбрасывает выбранный workspace и возвращает пользователя к `BrandSelector`; при одном бренде заново открывает рабочую форму этого бренда и сбрасывает внутренние вкладки/формы `BrandWorkspace`.

Текущая структура UI:

- верх экрана содержит кнопку `Назад` и отдельную белую плашку бренда с названием, человекочитаемой ролью и, если доступны управленческие permissions, кнопкой-шестерёнкой для настроек;
- flags/chips настроек бренда в плашке не показываются;
- рабочая зона показывает только один активный сценарий: экран поиска клиента или экран работы с выбранным клиентом;
- формы, options и результаты являются отдельными карточками;
- management-панели штампов, товаров, сотрудников и настроек бренда переиспользуют прежнюю логику, но открываются на отдельном экране настроек бренда;
- для списания штампов и выдачи товаров за монетки frontend сортирует уже полученные options так, чтобы доступные к списанию/выдаче награды шли первыми, затем недоступные с причиной нехватки.

Основные файлы:

- `src/StampService.Web/src/brands/BrandWorkspacePage.tsx`;
- `src/StampService.Web/src/brands/BrandSelector.tsx`;
- `src/StampService.Web/src/brands/BrandWorkspace.tsx`;
- `src/StampService.Web/src/brands/BrandCustomerSearchScreen.tsx`;
- `src/StampService.Web/src/brands/brandWorkspaceApi.ts`;
- `src/StampService.Application/Brands/Queries/GetBrandCustomerCard/*`;
- `src/StampService.Contracts/DTOs/Brands/BrandCustomerCardResponse.cs`;
- `src/StampService.Web/src/styles.css`.

## Web UI: актуальная навигационная модель карточек

Актуально на 2026-06-12.

В React Web UI упрощена навигация внутри основных пользовательских поверхностей. Новое правило: если элемент списка визуально является плашкой/карточкой сущности, сама плашка является основным действием открытия. Отдельные текстовые кнопки `Открыть` внутри таких плашек не нужны, если у элемента нет второго независимого действия.

Применено к кошельку пользователя:

- в `WalletPage` карточка бренда в списке `Мои награды` открывает кошелёк наград бренда по клику на всю плашку;
- заголовок пользовательской секции кошелька говорит о наградах, а не о брендах; счетчик брендов рядом с заголовком не используется, чтобы пустой кошелек не выглядел как список рабочих брендов;
- отдельная кнопка `Открыть` из карточки бренда удалена;
- карточка реализована как интерактивный элемент с hover/focus-состояниями в `src/StampService.Web/src/styles.css`;
- загрузка данных бренда по-прежнему идёт через существующий typed API client `getBrandDetails`, без изменений Application/API.

Также из web-экранов удалены локальные кнопки `Назад`. В текущей UX-модели возврат к корню раздела выполняется через основную навигацию приложения: повторный выбор раздела `Мой кошелёк` или `Работа`/`Бренды` сбрасывает соответствующую локальную вложенность. Это сохраняет правило home/root action для пунктов навигации и убирает дублирующие локальные back-controls.

Затронутые web-файлы:

- `src/StampService.Web/src/wallet/WalletPage.tsx`;
- `src/StampService.Web/src/wallet/BrandWalletPage.tsx`;
- `src/StampService.Web/src/brands/BrandWorkspacePage.tsx`;
- `src/StampService.Web/src/brands/BrandWorkspace.tsx`;
- `src/StampService.Web/src/styles.css`.

## OTP по телефону и SMS-доставка

Актуально на 2026-06-15.

Телефонный вход остается phone-first OTP-сценарием: Application создает и проверяет одноразовый код для нормализованного номера, а доставка кода является инфраструктурной деталью за портом `IPhoneAuthCodeSender`. Вход по телефону не должен зависеть от Telegram session state или UI-технических ids.

Для доставки OTP теперь используются два канала:

- Telegram-уведомление системному администратору остается базовым dev/admin-каналом и используется при обычной кнопке `Получить код`;
- SMS через SmsAero используется только когда клиент явно выбирает SMS-доставку на экране входа.

Разделение каналов является частью контракта запроса кода: `RequestPhoneAuthCodeRequest` содержит флаг `SendSms`. Если `SendSms=false`, код отправляется только администратору в Telegram. Если `SendSms=true`, код также отправляется клиенту по SMS, но только при включенной системной настройке SMS-кодов.

Системная настройка SMS-доставки хранится в БД как singleton `PhoneAuthSmsSettings`, а не только в `appsettings`. Это сделано, чтобы администратор мог включать/выключать SMS-коды из web-админки без изменения конфигурационных файлов. Конфигурация `SmsAero:SendAuthCodes` используется только как начальное/fallback-значение при первом создании singleton-настройки. Runtime-решение о доступности SMS должно проверять `IPhoneAuthSmsSettingsRepository`.

SmsAero credentials (`SmsAero:Login`, `SmsAero:ApiKey`) остаются инфраструктурной конфигурацией API host и должны задаваться через user-secrets/переменные окружения, а не храниться в репозитории. Текст SMS для клиента: `Код авторизации: {Код}`. Номер для SmsAero отправляется в формате без `+`.

Архитектурная раскладка:

- Domain: `PhoneAuthSmsSettings` описывает singleton-настройку включения SMS OTP;
- Application: `IPhoneAuthSmsSettingsRepository`, команды/запросы админки и проверка настройки в `AuthService` перед созданием SMS OTP;
- Infrastructure: EF mapping/repository, `SmsAeroPhoneAuthCodeSender`, `TelegramAdminPhoneAuthCodeSender`, `CompositePhoneAuthCodeSender`;
- API: публичное чтение доступности SMS для login UI и admin endpoints для чтения/изменения настройки;
- Web: login UI показывает отдельную кнопку SMS, а admin UI управляет настройкой через тумблер.

Важно: проверка настройки должна быть и во frontend для UX, и в backend/Application/Infrastructure для безопасности и консистентности. Если SMS выключены, кнопка SMS на login disabled, а серверный сценарий с `SendSms=true` возвращает доменную ошибку `auth.phone_sms_disabled` и не должен молча отправлять SMS.
