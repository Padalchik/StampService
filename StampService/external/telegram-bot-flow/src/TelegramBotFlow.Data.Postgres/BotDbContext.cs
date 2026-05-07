using Microsoft.EntityFrameworkCore;

namespace TelegramBotFlow.Core.Data;

/// <summary>
/// Generic DbContext for bot user storage.
/// Inherit to add custom user properties (ASP.NET Identity pattern):
/// <code>
/// public class AppDbContext : BotDbContext&lt;AppUser&gt; { }
/// </code>
/// </summary>
public class BotDbContext<TUser>(DbContextOptions options)
    : DbContext(options) where TUser : BotUser, new()
{
    public DbSet<TUser> Users => Set<TUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotDbContext<>).Assembly);
    }
}

/// <summary>
/// Default non-generic BotDbContext for simple bots that use BotUser as-is.
/// </summary>
public class BotDbContext(DbContextOptions<BotDbContext> options)
    : BotDbContext<BotUser>(options);