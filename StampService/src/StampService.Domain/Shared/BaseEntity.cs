namespace StampService.Domain.Shared;

public abstract class BaseEntity : ISoftDelete
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Явная реализация интерфейса — EF Core может записать, обычный код — нет
    DateTime? ISoftDelete.DeletedAt
    {
        get => DeletedAt;
        set => DeletedAt = value;
    }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}