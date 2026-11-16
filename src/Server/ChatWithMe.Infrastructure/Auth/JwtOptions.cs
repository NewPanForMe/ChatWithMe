namespace ChatWithMe.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public int RefreshTokenLifetimeDays { get; set; } = 7;

    public byte[] GetSigningKeyBytes()
    {
        if (string.IsNullOrWhiteSpace(SigningKey))
        {
            throw new InvalidOperationException(
                "缺少配置项 Jwt:SigningKey（HS256 对称密钥的 Base64 字符串，至少 32 字节）。请执行 dotnet user-secrets set \"Jwt:SigningKey\" \"<base64>\" 写入用户机密，或设置环境变量 Jwt__SigningKey。");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("配置项 Jwt:SigningKey 不是合法的 Base64 字符串。", exception);
        }

        if (key.Length < 32)
        {
            throw new InvalidOperationException("配置项 Jwt:SigningKey 解码后为 " + key.Length + " 字节，HS256 至少需要 32 字节。");
        }

        return key;
    }

    public void Validate()
    {
        GetSigningKeyBytes();

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("缺少配置项 Jwt:Issuer。");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("缺少配置项 Jwt:Audience。");
        }

        if (AccessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("配置项 Jwt:AccessTokenLifetimeMinutes 必须大于 0。");
        }

        if (RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException("配置项 Jwt:RefreshTokenLifetimeDays 必须大于 0。");
        }
    }
}
