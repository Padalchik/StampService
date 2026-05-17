using System.Security.Cryptography;

namespace StampService.Application.Auth;

public class PhoneAuthCodeGenerator : IPhoneAuthCodeGenerator
{
    public string Generate()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }
}
