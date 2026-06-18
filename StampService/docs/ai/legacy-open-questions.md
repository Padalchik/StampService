# Legacy, расхождения и открытые вопросы

Этот файл отделяет исторический контекст от актуальных правил. По умолчанию новые задачи должны читать `README.md` и профильный документ, а сюда заходить только при споре, миграции или подозрении на legacy.

## Старые большие файлы

- `OPERATIONAL_CONTEXT_CURRENT.md` и `WEB_UI_AGREEMENTS.md` больше не являются коротким стартовым контекстом.
- Они содержат много актуальных правил, но также дублируют друг друга и смешивают архитектуру, UI, историю bugfix-ов и рабочие инструкции.
- Их не удалять без разрешения владельца.
- Если старые файлы противоречат коду или `docs/ai`, ориентироваться на код и обновлять `docs/ai`.

## Убранные дубли

- Phone-first auth/OTP/SMS правила были в обоих старых файлах; теперь актуальная backend-часть в `backend-domain.md`, web-поведение в `web-ui.md`.
- Brand workspace/customer-card/welcome rewards были продублированы в обоих файлах; теперь backend/API границы в `backend-domain.md`, UX и component model в `web-ui.md`.
- Wallet tabs `Штампы` / `Монетки` / `История` были описаны несколько раз; теперь правило в `web-ui.md`, кодовые входы в `code-map.md`.
- Demo reward notifications были в обоих файлах; теперь правило в `backend-domain.md`, без подробной истории bugfix-а.
- Списки файлов из старых документов заменены компактной картой в `code-map.md`.

## Legacy, которое не возвращать без отдельного решения

- `CustomerCode` как 4-значный клиентский идентификатор не является частью актуальной модели `User`.
- Старые начисления по `CustomerCode` не восстанавливать.
- Новые DTO, формы, фильтры, ссылки и UI-типы вокруг `customerCode` не проектировать.
- Telegram-only аккаунты не создавать в штатных сценариях.
- Миграция legacy Telegram-only аккаунтов допустима только как отдельный согласованный migration flow.
- Static/prototype web surfaces `/app`, `/phone-auth-test`, `/design-variants` не восстанавливать.
- Удаленные endpoints вроде старого staff add без phone-first flow и старого metric issue без `issue-by-phone` не поддерживать без отдельного решения.

## Расхождения старых формулировок с кодом

- Старые документы местами формулируют `GET /api/brands/{brandId}/customer-card` как будто Application query сам возвращает lookup DTO `found=false`. В коде `GetBrandCustomerCardHandler` возвращает `RecipientNotFound`, а `BrandsController.GetCustomerCard` превращает только эту ошибку в `BrandCustomerCardLookupResponse(false, null)`.
- Старые документы говорят, что Telegram не первичный способ создания аккаунта. Это актуально. При этом endpoint `POST /api/auth/telegram` существует: он логинит только уже привязанную Telegram identity и требует активную phone identity у пользователя.
- Старые документы называют web UI "простой web UI" и одновременно описывают полноценное React-приложение. Актуальное направление - развивать `src/StampService.Web` как React/TypeScript app.

## Нужно проверить

- Нужно проверить актуальность VS Code launch configuration `App: полный запуск в браузере`; в этой задаче `.vscode/launch.json` не читался.
- Нужно проверить, есть ли еще runtime/DB следы legacy `customer_code` только в старых migrations или где-то в текущих repository/query paths; быстрый кодовый обзор не выявил актуального `User.CustomerCode`, но полный аудит migrations не выполнялся.
- Нужно проверить фактическую конфигурацию admin access: старые документы акцентируют `Admin:TelegramUserIds`, а в кодовой карте также виден `AdminOptions.PhoneNumbers`. Полная политика админ-доступа не разбиралась до деталей.
- Нужно проверить, что все Telegram flows уже полностью соответствуют phone-first правилам; прочитан `ProfileEndpoint` и карта features, но не каждый action/screen.
- Нужно проверить, не появились ли после 2026-06-17 новые UI-библиотеки или дизайн-соглашения; в этой задаче анализировался текущий локальный код без запуска frontend.

## Что проверялось в этой задаче

- Старые документы прочитаны с UTF-8.
- Структура проекта просмотрена через `rg --files`.
- Точечно прочитаны ключевые Domain/Application/API files по User, Brand, BrandCustomer, Auth, Wallet, Ledger, API brands и demo notifications.
- React и Telegram слои проверялись через targeted search и ключевые файлы/совпадения, без запуска.

## Что не запускалось

- Backend/API host.
- Telegram bot.
- React/Vite dev server.
- Build.
- Tests.
- EF migrations.
