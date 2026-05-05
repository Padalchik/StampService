using StampService.Domain.User;

namespace StampService.Application.Auth;

public interface IJwtTokenService
{
    JwtToken CreateToken(User user);
}
