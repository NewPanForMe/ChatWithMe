using System.Security.Cryptography;
using System.Text;
using ChatWithMe.Application.Abstractions;
using ChatWithMe.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatWithMe.Infrastructure.Storage;

/// <summary>
/// 文件回读链接的 HMAC 签名器。密钥由 <c>Jwt:SigningKey</c> 派生出独立用途的子密钥
/// （"ChatWithMe:file-link:v1"），不额外引入新的秘密配置；签名绑定文件 Id 与过期时刻。
/// </summary>
public sealed class FileLinkSigner : IFileLinkSigner
{
    private const string Purpose = "ChatWithMe:file-link:v1";

    private readonly IClock _clock;
    private readonly byte[] _key;

    public FileLinkSigner(IOptions<JwtOptions> jwtOptions, IClock clock)
    {
        _clock = clock;

        using var hmac = new HMACSHA256(jwtOptions.Value.GetSigningKeyBytes());
        _key = hmac.ComputeHash(Encoding.UTF8.GetBytes(Purpose));
    }

    public string CreateSignature(Guid fileAssetId, DateTime expiresAtUtc)
    {
        var payload = BuildPayload(fileAssetId, expiresAtUtc);

        using var hmac = new HMACSHA256(_key);

        return Base64UrlEncoder.Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    public bool Verify(Guid fileAssetId, DateTime expiresAtUtc, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || expiresAtUtc <= _clock.UtcNow)
        {
            return false;
        }

        var expected = CreateSignature(fileAssetId, expiresAtUtc);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature));
    }

    private static string BuildPayload(Guid fileAssetId, DateTime expiresAtUtc)
    {
        var expiresAtUnix = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();

        return Purpose + "\n" + fileAssetId.ToString("N") + "\n" + expiresAtUnix;
    }
}
