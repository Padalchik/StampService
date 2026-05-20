using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FluentResults;
using Microsoft.Extensions.Configuration;
using StampService.Application.Errors;
using StampService.Application.Users;

namespace StampService.Infrastructure.Services;

public class CompactTelegramLinkSessionProtector : ITelegramLinkSessionProtector
{
    private const string Prefix = "l_";
    private const int PayloadLength = 29;
    private const int SignatureLength = 16;
    private const int TokenBytesLength = PayloadLength + SignatureLength;

    private readonly IConfiguration _configuration;

    public CompactTelegramLinkSessionProtector(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Protect(TelegramLinkSession session)
    {
        Span<byte> payload = stackalloc byte[PayloadLength];
        payload[0] = 1;
        session.UserId.TryWriteBytes(payload[1..17]);
        BinaryPrimitives.WriteInt64BigEndian(
            payload[17..25],
            new DateTimeOffset(session.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds());
        RandomNumberGenerator.Fill(payload[25..29]);

        var signature = Sign(payload);
        var tokenBytes = new byte[TokenBytesLength];
        payload.CopyTo(tokenBytes.AsSpan());
        signature.AsSpan().CopyTo(tokenBytes.AsSpan(PayloadLength));

        return Prefix + Base64UrlEncode(tokenBytes);
    }

    public Result<TelegramLinkSession> Unprotect(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(Prefix, StringComparison.Ordinal))
            return Result.Fail(AuthErrors.TelegramCodeInvalid());

        byte[] tokenBytes;
        try
        {
            tokenBytes = Base64UrlDecode(token[Prefix.Length..]);
        }
        catch
        {
            return Result.Fail(AuthErrors.TelegramCodeInvalid());
        }

        if (tokenBytes.Length != TokenBytesLength || tokenBytes[0] != 1)
            return Result.Fail(AuthErrors.TelegramCodeInvalid());

        var payload = tokenBytes.AsSpan(0, PayloadLength);
        var signature = tokenBytes.AsSpan(PayloadLength, SignatureLength);
        var expectedSignature = Sign(payload);
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
            return Result.Fail(AuthErrors.TelegramCodeInvalid());

        var userId = new Guid(payload.Slice(1, 16));
        var expiresAtUtc = DateTimeOffset
            .FromUnixTimeSeconds(BinaryPrimitives.ReadInt64BigEndian(payload.Slice(17, 8)))
            .UtcDateTime;

        return Result.Ok(new TelegramLinkSession(userId, expiresAtUtc));
    }

    private byte[] Sign(ReadOnlySpan<byte> payload)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return hmac.ComputeHash(payload.ToArray()).Take(SignatureLength).ToArray();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');

        return Convert.FromBase64String(padded);
    }
}
