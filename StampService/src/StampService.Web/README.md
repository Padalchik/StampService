# StampService Web

React frontend для StampService.

## Назначение

Первый этап web-приложения покрывает вход по телефону через существующие API endpoints:

- `POST /api/auth/phone/code`
- `POST /api/auth/phone/verify`

После успешной проверки кода backend возвращает JWT, который frontend использует для следующих authenticated-запросов.

В приложении уже есть базовый роутинг:

- `/login` - вход по телефону;
- `/` - защищённая оболочка приложения с левым меню.

## Запуск

Backend API должен быть запущен отдельно.

### Через VS Code

1. Открыть корень репозитория в VS Code.
2. Перейти в `Run and Debug`.
3. Выбрать конфигурацию `App: полный запуск в браузере`.
4. Нажать Start.

VS Code запустит backend API, Telegram bot и Vite dev server. Если `node_modules` ещё нет, зависимости будут установлены автоматически перед стартом.

PostgreSQL должен быть уже запущен отдельно.

### Через терминал

```powershell
npm install
npm run dev
```

Vite dev server проксирует `/api` на `https://localhost:7247`. Если backend запущен на другом адресе, нужно изменить `server.proxy` в `vite.config.ts`.

## Авторизация

JWT-логика изолирована в `src/auth` и `src/api`.

На текущем этапе токен хранится в `localStorage`, потому что backend уже выдаёт bearer JWT и пока не реализует httpOnly cookie/refresh-token flow. Это осознанное временное решение для первого этапа. Перед production hardening нужно отдельно обсудить переход на cookie/refresh-token модель или другой серверный session flow.

Правила:

- не логировать JWT;
- не передавать JWT в компоненты напрямую;
- все API-запросы выполнять через общий `apiRequest`;
- `logout` должен очищать локальное auth-хранилище.
