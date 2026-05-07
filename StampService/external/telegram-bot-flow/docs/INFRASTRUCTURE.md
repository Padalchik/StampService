# Инфраструктура (Infrastructure)

## Обзор компонентов (Component Overview)

| Компонент / Component | Образ / Image          | Порт / Port | Назначение / Purpose                                                                 |
| --------------------- | ---------------------- | ----------- | ------------------------------------------------------------------------------------ |
| Bot                   | Собственный Dockerfile | —           | Telegram-бот (ASP.NET Core) / Telegram bot (ASP.NET Core)                            |
| PostgreSQL            | `postgres:16-alpine`   | 5432        | Хранение данных / Data storage (рассылки / broadcasts, пользователи / users, Quartz) |
| Seq                   | `datalust/seq:latest`  | 5341/8081   | Структурированные логи / Structured logs                                             |

## Docker Compose

### Разработка — только инфраструктура (Development — Infrastructure Only)

```bash
docker compose -f docker-compose-infra.yml up -d
```

Запускает: PostgreSQL, Seq

Starts: PostgreSQL, Seq

### Production — полный стек (Production — Full Stack)

```bash
cp .env.example .env
# Заполнить BOT_TOKEN и DB_PASSWORD в .env
# Fill in BOT_TOKEN and DB_PASSWORD in .env

docker compose up -d --build
```

Запускает: Bot, PostgreSQL, Seq

Starts: Bot, PostgreSQL, Seq

## Переменные окружения (Environment Variables)

| Переменная / Variable | Описание / Description                   | Пример / Example     |
| --------------------- | ---------------------------------------- | -------------------- |
| `BOT_TOKEN`           | Токен Telegram-бота / Telegram bot token | `123456789:ABC...`   |
| `DB_PASSWORD`         | Пароль PostgreSQL / PostgreSQL password  | `my_secure_password` |

## PostgreSQL

### Connection string (Строка подключения)

```
Host=localhost;Database=telegram_bot_flow;Username=botflow;Password=<DB_PASSWORD>
```

Для запуска в Docker (внутри сети compose) используется host `postgres`:

For Docker deployment (inside the compose network), use host `postgres`:

```
Host=postgres;Database=telegram_bot_flow;Username=botflow;Password=<DB_PASSWORD>
```

### Базы данных (Databases)

- `telegram_bot_flow` — основная БД / main DB (таблицы рассылок / broadcast tables + Quartz)

`users` хранится в этой же БД через `TelegramBotFlow.Core.Data`.

`users` is stored in the same DB via `TelegramBotFlow.Core.Data`.

### Таблицы (Tables)

| Таблица / Table            | Назначение / Purpose                                                      |
| -------------------------- | ------------------------------------------------------------------------- |
| `users`                    | Отслеживание пользователей бота / Bot user tracking                       |
| `broadcasts`               | Ручные рассылки / Manual broadcasts                                       |
| `broadcast_sequences`      | Последовательности рассылок / Broadcast sequences                         |
| `broadcast_sequence_steps` | Шаги последовательностей / Sequence steps                                 |
| `user_sequence_progress`   | Прогресс пользователей в последовательностях / User progress in sequences |
| `qrtz_*`                   | Таблицы Quartz.NET Scheduler / Quartz.NET Scheduler tables                |

## Seq — логирование (Logging)

- UI: http://localhost:8081
- API: http://localhost:5341
- Данные сохраняются в volume `seq-data` / Data persisted in volume `seq-data`

## Volumes (Тома)

| Volume / Том    | Назначение / Purpose                |
| --------------- | ----------------------------------- |
| `seq-data`      | Данные Seq / Seq data               |
| `postgres-data` | Данные PostgreSQL / PostgreSQL data |
