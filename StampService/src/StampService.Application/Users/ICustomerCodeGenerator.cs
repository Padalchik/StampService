namespace StampService.Application.Users;

public interface ICustomerCodeGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken);
}
