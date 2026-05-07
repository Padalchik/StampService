# TelegramBotFlow — Интеграционные тесты

## Обзор

Интеграционные тесты для проверки запуска приложения, разрешения зависимостей, работы middleware pipeline и взаимодействия с Telegram API.

## Структура тестов

### 📁 Application/
Тесты для проверки запуска приложения и интеграции компонентов:

- **ApplicationStartupTests** — запуск приложения, разрешение зависимостей (singleton/scoped), конфигурация
- **EndpointsRegistrationTests** — регистрация эндпоинтов, проверка transient lifetime, обнаружение из сборки

### 📁 Sessions/
Тесты для проверки работы с сессиями:

- **RedisSessionStoreIntegrationTests** — интеграция с Redis через Testcontainers

### 📁 Infrastructure/
Инфраструктура для тестов:

- **BotWebApplicationFactory** — WebApplicationFactory с мокированным ITelegramBotClient
- **RedisFixture** — фикстура для поднятия Redis-контейнера
- **IntegrationTestsCollection** — коллекции тестов для xUnit

## Технологии

- **xUnit** — фреймворк для тестирования
- **FluentAssertions** — читаемые assertion'ы
- **NSubstitute** — мокирование Telegram API
- **Microsoft.AspNetCore.Mvc.Testing** — WebApplicationFactory для интеграционных тестов
- **Testcontainers** — Docker-контейнеры для тестов (Redis)

## Запуск тестов

### Все тесты
```bash
dotnet test
```

### Конкретная категория
```bash
# Только тесты запуска приложения
dotnet test --filter "FullyQualifiedName~ApplicationStartupTests"

# Только тесты эндпоинтов
dotnet test --filter "FullyQualifiedName~EndpointsRegistrationTests"

# Только тесты с Redis
dotnet test --filter "FullyQualifiedName~RedisSessionStoreIntegrationTests"
```

### С покрытием кода
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Мокирование Telegram API

Все тесты используют мокированный `ITelegramBotClient` через NSubstitute.

В `BotWebApplicationFactory` реальный Telegram Bot Client подменяется на мок:

```csharp
public class BotWebApplicationFactory : WebApplicationFactory<Program>
{
    public ITelegramBotClient MockTelegramBotClient { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Подменяем ITelegramBotClient на мок
        services.RemoveAll<ITelegramBotClient>();
        services.AddSingleton(MockTelegramBotClient);
        
        // Отключаем PollingService для тестов
        services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));
    }
}
```

Это позволяет тестировать приложение без реальных вызовов к Telegram API.

## Что проверяется

### ✅ Запуск приложения
- Успешный старт WebApplication
- Разрешение всех зависимостей
- Правильные lifetime (singleton/scoped/transient)
- Отсутствие конфликтов зависимостей

### ✅ Эндпоинты
- Регистрация всех IBotEndpoint из сборки TelegramBotFlow.App
- Transient lifetime эндпоинтов
- Наличие метода MapEndpoint с правильной сигнатурой
- Обнаружение всех конкретных типов эндпоинтов
- Уникальность регистрации каждого типа

### ✅ Telegram API
- Подмена ITelegramBotClient на мок через NSubstitute
- Изоляция от реального Telegram API
- Использование фейковой конфигурации для тестов

### ✅ Конфигурация
- Загрузка in-memory конфигурации
- Доступность всех настроек через IConfiguration

### ✅ Redis Session Store
- Сохранение и загрузка сессий
- TTL и истечение сессий
- Concurrent access
- Cleanup старых сессий

## Требования

- **.NET 10.0 SDK**
- **Docker** (для Testcontainers)

## CI/CD

Тесты должны выполняться в CI/CD pipeline:

```yaml
- name: Run Integration Tests
  run: dotnet test --no-build --verbosity normal
```

Для Redis-тестов требуется Docker в CI-окружении.

## Добавление новых тестов

### 1. Создай класс теста
```csharp
[Collection(nameof(BotApplicationTests))]
public class MyNewTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly BotWebApplicationFactory _factory;
    
    public MyNewTests(BotWebApplicationFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public void My_Test()
    {
        // Arrange
        var service = _factory.Services.GetRequiredService<MyService>();
        
        // Act
        var result = service.DoSomething();
        
        // Assert
        result.Should().NotBeNull();
    }
}
```

### 2. Используй существующие фикстуры
- `BotWebApplicationFactory` — для тестов приложения
- `RedisFixture` — для тестов с Redis

### 3. Используй helper-методы
Для создания тестовых Update-объектов используй существующие helper-методы в тестах pipeline.

## Полезные команды

```bash
# Запуск в watch-режиме
dotnet watch test

# Запуск с детальным выводом
dotnet test --logger "console;verbosity=detailed"

# Запуск только быстрых тестов (без Redis)
dotnet test --filter "FullyQualifiedName!~Redis"
```
