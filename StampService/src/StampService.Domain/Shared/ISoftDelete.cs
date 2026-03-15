namespace StampService.Domain.Shared;

public interface ISoftDelete
{
    DateTime? DeletedAt { get; set; }
}