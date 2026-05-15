# Operational Context For Next Chat

Дата актуализации: 2026-05-15, Europe/Moscow.

## 1. Что за проект

StampService - .NET-сервис лояльности с основным UX через Telegram-бота.

Цель MVP: дать бизнесу простой инструмент для брендов, клиентов, начисления/списания бонусов, просмотра балансов, истории и доступных наград без тяжёлой админки.

Архитектура сейчас - layered / modular monolith:

- `src/StampService.Domain` - доменные сущности и правила.
- `src/StampService.Application` - use cases, commands/queries, интерфейсы репозиториев, сервисы доступа.
- `src/StampService.Contracts` - DTO.
- `src/StampService.Infrastructure` - EF Core, PostgreSQL, конфигурации, репозитории, миграции, seeding.
- `src/StampService.API` - HTTP API.
- `src/StampService.TelegramBot` - Telegram UX, screens/endpoints, фоновые задачи бота.
- `external/telegram-bot-flow` - локальная библиотека Telegram flow/navigation.
- `Tests/*` - доменные и application-тесты.

Ключевые домены:

- `User`, `UserIdentity`, `RedemptionCode`
- `Brand`, `BrandMembership`, `Role`
- `LoyaltyMetricDefinition`, `MetricBalance`, `StampTransaction`
- `CoinWallet`, `CoinTransaction`, `CoinProduct`
- `CustomerDigestState`, `RewardDigestSettings`

## 2. Рабочие договорённости

- Пользователь говорит по-русски. Отвечать по-русски.
- Не запускать build/test без явного запроса пользователя. Исключение - если пользователь сам принёс ошибку компиляции и просит исправить; всё равно лучше объяснить, что запускается точечная проверка.
- Миграции создавать только через `dotnet ef migrations add`. Не писать migration-файлы вручную.
- Не откатывать чужие изменения в worktree без явного разрешения.
- Для ручных правок использовать `apply_patch`, не писать файлы через shell redirection/cat.
- Перед изменениями сначала читать существующие паттерны проекта.
- При работе с русским текстом следить за UTF-8. В проекте ранее встречались "кракозябры"; новые строки должны быть нормальным русским текстом.
- `NU1900` от NuGet audit при закрытой сети может быть инфраструктурным шумом. Если нужен build, можно собирать в отдельный `OutputPath`, чтобы не конфликтовать с запущенным ботом.

Полезные команды:

```powershell
git -c safe.directory=C:/Programmer/StampService status --short
git -c safe.directory=C:/Programmer/StampService diff --check
rg -n "<pattern>" src Tests -g "*.cs"
```

EF migration из корня проекта:

```powershell
dotnet ef migrations add <Name> -p .\src\StampService.Infrastructure -s .\src\StampService.API
```

Применение миграций:

```powershell
dotnet ef database update -p .\src\StampService.Infrastructure -s .\src\StampService.API
```

## 3. Важные архитектурные решения

### Микросервисы

Микросервисы сейчас не делаем.

Причина: проект на стадии MVP, пользователей/нагрузки пока нет, а микросервисы добавят лишнюю сложность: отдельный деплой, распределённые транзакции, контракты, ретраи, мониторинг, трассировка, синхронизация данных.

Текущий путь: modular monolith.

### Notification Outbox

Outbox обсуждался как будущий паттерн для надёжной доставки уведомлений, но пользователь решил откатить эту работу, потому что не хочет терять понимание проекта и полагаться на "магию".

Текущее правило: уведомления отправляются напрямую через Telegram из `StampService.TelegramBot/Common/Notifications`.

Outbox сейчас не реализовывать без нового явного решения пользователя.

### Demo data seeding

Обсуждался механизм demo-базы.

Принятые упрощения:

- одна БД = либо demo, либо real;
- механизм seeding ничего не удаляет;
- пользователь сам вручную очищает БД, применяет миграции и только потом запускает заполнение;
- seeder должен просто заполнить пустую БД данными с нуля;
- можно добавить защитную проверку "если в БД уже есть пользователи/бренды - остановиться", но не делать reset/cleanup.

Рекомендуемый путь, если пользователь попросит реализовать:

- `src/StampService.Infrastructure/Seeding/DemoDataSeeder.cs`;
- запуск через `src/StampService.API` специальным аргументом, например `--seed-demo`;
- после выполнения seed процесс завершает работу, не поднимая постоянный сервис;
- перед demo seed вызвать/гарантировать `RoleSeeder.SeedSystemRolesAsync`.

Пока demo seeder НЕ реализован.

## 4. Telegram UX

### Главное меню

В главном меню:

- `Мой кошелёк`
- `Админка`, если пользователь админ
- если один рабочий бренд - кнопка бренда
- если несколько рабочих брендов - `Рабочие бренды`

Не добавлять лишнюю кнопку `Главное меню` на самом главном меню.

### Мой кошелёк

`MyWalletScreen` показывает:

- код пользователя;
- одноразовый код списания;
- срок действия кода;
- балансы по брендам;
- доступные награды по брендам;
- кнопку обновления данных.

Правила:

- при открытии кошелька обновляется `LastWalletOpenedAtUtc` для digest-логики;
- кнопка обновления кошелька должна обновлять код списания по правилам ротации;
- в кошельке показывать только доступные товары/метрики;
- недоступные награды пользователь видит при проваливании в бренд;
- для бренда в кошельке используется эмодзи `▶️`.

### Уведомления

Прямые клиентские уведомления отправляются из:

- `CustomerNotificationService`
- `CustomerRewardDigestSender`

После отправки уведомления сбрасывается nav message, чтобы следующий экран открывался новым сообщением, а не перерисовывал старое окно под уведомлением.

Digest-уведомление должно иметь кнопку `Мой кошелёк`, которая ведёт в обычный `MyWalletScreen` по стандартному `nav:my_wallet`, чтобы пользователь сразу попадал в общий поток бота.

## 5. Бренды и настройки бренда

У бренда есть настройки:

- учитывать метрики;
- учитывать монетки;
- списание монеток за конкретные товары;
- ручное/произвольное списание монеток, если включено соответствующей настройкой.

Важное правило: если метрики или монетки отключены у бренда, в пользовательском UX не должно быть упоминаний отключённого раздела.

Настройки включения/отключения метрик/монет и режимов списания должны управляться владельцем в управлении брендом, не через глобальную админку.

В админке есть редактирование бренда/владельца, но brand feature toggles относятся к управлению брендом владельцем.

## 6. Монетки и товары за монетки

Монетки (`Coin`) - отдельный поддомен, не метрика.

Реализовано:

- `CoinWallet`
- `CoinTransaction`
- начисление монеток;
- списание монеток;
- история монеток;
- товары за монетки (`CoinProduct`);
- покупка товара за монетки;
- ручное списание монеток как отдельный режим, если включено настройками бренда.

В purchase flow:

1. сотрудник вводит код списания клиента;
2. бот показывает товары;
3. кнопки товаров показывают баланс/цену, например `Кофе · 7/10`;
4. если монеток не хватает, товар не должен списываться;
5. если хватает, сначала экран подтверждения;
6. списание только после подтверждения;
7. отмена отменяет сценарий.

Комментарий в coin transaction при покупке товара должен быть названием товара, без технического префикса.

`ActorUserId` не показывать пользователю в истории. Это технический аудит.

## 7. Метрики

Метрики бренда реализованы:

- список метрик;
- детали метрики;
- создание;
- редактирование;
- начисление клиенту;
- списание;
- история;
- балансы клиента.

Служебное название/code метрики удалено из UX. В `LoyaltyMetricDefinition` используются пользовательское название и `RedemptionAmount`.

## 8. История транзакций

В истории транзакций клиента нужно разделять:

- монеты;
- метрики.

Если монеты или метрики выключены у бренда, соответствующий блок не показывать.

История должна быть пользовательской:

- не показывать `ActorUserId`;
- не показывать технические поля;
- комментарии показывать только если они полезны пользователю.

## 9. Код списания

`RedemptionCode` - одноразовый код списания, который клиент показывает сотруднику.

Текущее правило: время жизни кода списания - 5 минут.

В UX писать `код пользователя`, не `CustomerCode`.

`CustomerCode` как техническое имя в коде можно оставлять.

## 10. Reward Digest

Реализован системный дайджест доступных наград для клиента.

Это не брендовая рассылка. Бренды не управляют отправкой напрямую.

Условия отправки:

- у клиента есть хотя бы одна доступная награда;
- награда принадлежит активному бренду;
- награда активна;
- баланса клиента достаточно;
- с последнего digest прошло не меньше `MessageToUserIntervalMinutes`;
- с последнего открытия кошелька прошло не меньше `MessageToUserIntervalMinutes`.

Состояние:

- `CustomerDigestState.LastDigestSentAtUtc`
- `CustomerDigestState.LastWalletOpenedAtUtc`

Настройки:

- `RewardDigestSettings.Enabled`
- `MessageToUserIntervalMinutes`
- `ScanIntervalMinutes`
- `BatchSize`
- `MaxBrandsPerMessage`
- `MaxRewardsPerBrand`

Настройки хранятся в БД в `reward_digest_settings` и редактируются через админку без перезапуска.

`RewardDigest` убран из `appsettings.json`. `RewardDigestOptions` остаётся только как fallback для первого создания записи настроек, если строки в БД ещё нет.

`CustomerRewardDigestHostedService` - background service, который периодически проверяет пользователей.

Важно различать:

- `ScanIntervalMinutes` - как часто система сканирует кандидатов;
- `MessageToUserIntervalMinutes` - как часто одному пользователю можно отправлять digest.

Чтобы изменения из админки применялись без перезапуска, hosted service перечитывает настройки регулярно, не засыпая навсегда на старое значение.

## 11. Админка

В админке есть:

- управление брендами;
- создание бренда;
- смена владельца;
- экран `Дайджест наград` для глобальных настроек digest.

Настройки digest редактируются через админку:

- включить/выключить;
- интервал сообщений пользователю;
- интервал проверки;
- batch size;
- максимум брендов в сообщении;
- максимум наград на бренд.

## 12. TelegramBotFlow

Локальная библиотека: `external/telegram-bot-flow`.

Важные правила:

- `IBotAction` находится в `TelegramBotFlow.Core.Endpoints`;
- `ScreenView.NavigateButton<TScreen>()` создаёт callback `nav:<screen_id>`;
- для `MyWalletScreen` screen id по convention - `my_wallet`;
- action-view отличается от обычного screen stack;
- при странном поведении back/navigation сначала смотреть `external/telegram-bot-flow`.

## 13. Миграции и текущие миграционные нюансы

Пользователь ранее подчёркивал: миграции делать только через `dotnet ef migrations add`.

Если добавляется новая таблица/поле, сообщить, что нужна миграция, и предложить команду.

Не писать migration вручную.

## 14. Последние важные решения

- Outbox был предложен, частично реализовывался, но пользователь решил откатить. Не возвращать без явного запроса.
- Микросервисы не делать сейчас.
- Demo seeding нужен простой: только заполнение пустой БД, без удаления данных.
- Build/test не запускать без явного запроса.
- Следующая вероятная задача: реализовать `DemoDataSeeder` для пустой БД.

## 15. Если новый чат начнёт с DemoDataSeeder

Рекомендуемый план:

1. Прочитать доменные фабрики (`Create`) для `User`, `Brand`, `BrandMembership`, `CoinProduct`, `LoyaltyMetricDefinition`, `CoinWallet`, `MetricBalance`, transactions.
2. Сделать `DemoDataSeeder` в `StampService.Infrastructure/Seeding`.
3. Seeder должен предполагать пустую БД.
4. В начале проверить, что нет пользователей/брендов; если есть - бросить понятную ошибку.
5. Создать системные роли через `RoleSeeder`.
6. Создать демо-пользователей с фиксированными customer code.
7. Создать 2-3 бренда с разными настройками:
   - бренд с монетками и метриками;
   - бренд только с монетками;
   - бренд только с метриками.
8. Создать owner/staff memberships.
9. Создать товары за монетки и метрики.
10. Создать кошельки/балансы/историю так, чтобы в кошельке были доступные и недоступные награды.
11. Добавить запуск через `StampService.API` аргументом `--seed-demo`; после seed приложение должно завершаться.
12. Если нужны новые миграции - только через `dotnet ef migrations add`.

## 16. Current Code State Notes, 2026-05-15

This section is intentionally ASCII-only to avoid changing the existing Russian text encoding.

### Brand creation

Brand creation is now only allowed together with owner assignment.

Current path:

- Application command: `CreateBrandWithOwnerCommand` / `CreateBrandWithOwnerHandler`.
- Owner change path: `ReassignBrandOwnerCommand` / `ReassignBrandOwnerHandler`.
- Telegram admin UX uses the admin brand flow.

Removed legacy path:

- `CreateBrandCommand`
- `CreateBrandHandler`
- `CreateBrandRequest`
- `CreateBrandResponse`
- `AssignBrandOwnerCommand`
- `AssignBrandOwnerHandler`
- `AssignBrandOwnerRequest`
- `AssignBrandOwnerResponse`
- HTTP endpoints `POST /api/brands` and `POST /api/brands/{brandId}/owner`

Do not restore the removed legacy brand flow unless the user explicitly asks for a new design.

### Shared helpers added

API:

- `src/StampService.API/Controllers/ApiControllerBase.cs`
- It centralizes reading current authenticated `UserId` from JWT claims.
- `BrandsController`, `CoinProductsController`, `MetricsController`, and `UsersController` use it.

TelegramBot:

- `src/StampService.TelegramBot/Common/Routing/BotEndpointHelpers.cs`
- It centralizes common endpoint helpers such as `EnsureUserAsync`, retry input views, and error views.
- Some existing endpoints still have local helper code; prefer moving repeated endpoint glue into `BotEndpointHelpers` during nearby edits.