namespace StampService.Application.Demo;

public interface IDemoDatabaseResetService
{
    Task ResetAsync(CancellationToken cancellationToken);
}
