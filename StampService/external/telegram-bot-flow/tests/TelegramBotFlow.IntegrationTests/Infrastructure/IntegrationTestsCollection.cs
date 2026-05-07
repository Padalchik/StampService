namespace TelegramBotFlow.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(RedisIntegrationTests))]
public class RedisIntegrationTests : ICollectionFixture<RedisFixture>;

[CollectionDefinition(nameof(BotApplicationTests))]
public class BotApplicationTests : ICollectionFixture<BotWebApplicationFactory>;