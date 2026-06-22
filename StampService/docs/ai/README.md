# StampService AI-start

Актуально на 2026-06-17. Это короткий стартовый файл для нового AI-чата по репозиторию `C:\Programmer\StampService\StampService`.

## Как читать

Сначала прочитай этот файл. Дальше открывай только документы под тип задачи:

- backend/domain/API/Telegram/ledger/welcome rewards: `docs/ai/backend-domain.md`;
- React Web UI, мобильная верстка, brand workspace, wallet, админка: `docs/ai/web-ui.md`;
- где искать код и какие endpoints/handlers связаны: `docs/ai/code-map.md`;
- legacy, исторические решения, расхождения старых документов с кодом, открытые вопросы: `docs/ai/legacy-open-questions.md`.

Старые большие файлы `OPERATIONAL_CONTEXT_CURRENT.md` и `WEB_UI_AGREEMENTS.md` оставлены как архивный источник. Не читай их целиком по умолчанию. Если новая документация и старые файлы расходятся, сначала сверяйся с кодом, затем обновляй `docs/ai`.

## Рабочие правила

- Общаться с владельцем проекта на русском.
- Перед правками читать существующий код и следовать локальным паттернам.
- Для поиска использовать `rg` / `rg --files`.
- Production-код не менять, если задача только про документацию.
- Не запускать backend, Telegram bot, frontend dev server, build, tests или migrations без явного разрешения.
- EF migrations не писать вручную; если миграция нужна, создавать через `dotnet ef migrations add` только после разрешения.
- Не откатывать чужие изменения в рабочем дереве без явного разрешения.
- Проект остается modular monolith; не предлагать микросервисы без отдельного архитектурного решения.

## Суть проекта

StampService - loyalty-сервис для брендов. Основные поверхности: HTTP API, Telegram bot и React Web UI.

Ключевая модель: один человек = один `User.Id`; внешние способы входа и связи живут в `UserIdentity` (`Phone`, `Telegram`). Бизнес-данные, роли, балансы и история принадлежат `User.Id`, а не телефону или Telegram id.

Телефон - первичная identity для клиента. Вход подтверждается OTP по телефону. Telegram - вторичная identity: вход/связь/уведомления после привязки к телефонному аккаунту. Новые Telegram-only аккаунты не должны появляться в штатных сценариях.

Клиент бренда - не просто глобальный `User`, а связка `BrandCustomer` (`BrandId + UserId`). Поиск клиента в workspace, кошелек бренда и операции должны быть scoped конкретным брендом.

Лояльность ведется через ledger: для штампов используются `MetricBalance` + `StampTransaction`, для монеток `CoinWallet` + `CoinTransaction`. Materialized balance/wallet не является источником истины и должен синхронизироваться с ledger-транзакциями.

## Быстрая маршрутизация задач

- Auth/OTP/SMS/profile/identity: читай `backend-domain.md` и код `src/StampService.Application/Auth`, `src/StampService.Application/Users`, `src/StampService.API/Controllers/AuthController.cs`, `UsersController.cs`.
- Brand customer/customer card/welcome rewards: читай `backend-domain.md`, `web-ui.md`, `code-map.md`.
- Ledger/штампы/монетки/products/redeem codes: читай `backend-domain.md` и `code-map.md`.
- React UI/layout/mobile/navigation/texts: читай `web-ui.md`.
- Telegram bot parity: читай `backend-domain.md`, `code-map.md`, затем соответствующие `src/StampService.TelegramBot/Features/*`.
- Demo/admin/audit/diagnostics: читай `backend-domain.md`, `code-map.md`, при спорных деталях `legacy-open-questions.md`.

## Проверки после изменений

Если владелец явно не разрешил запуск, не запускай проверки. В финальном ответе прямо напиши, что build/tests/dev servers/migrations не запускались.
