using System.Security.Cryptography;
using StampService.Domain.User;

namespace StampService.Application.Users;

public class CustomerCodeGenerator : ICustomerCodeGenerator
{
    private const int MaxAttempts = 25;

    private readonly IUserRepository _userRepository;

    public CustomerCodeGenerator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var customerCode = RandomNumberGenerator
                .GetInt32(0, 10_000)
                .ToString($"D{User.CustomerCodeLength}");

            var exists = await _userRepository.CustomerCodeExistsAsync(
                customerCode,
                cancellationToken);

            if (!exists)
                return customerCode;
        }

        throw new InvalidOperationException("Could not generate unique customer code");
    }
}
