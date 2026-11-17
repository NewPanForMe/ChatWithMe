namespace ChatWithMe.Infrastructure.ConnectionStrings;

/// <summary>
/// 连接串解密密钥（AES-256-GCM）的唯一来源。与 <c>Jwt:SigningKey</c> 一样属于"配置错误"而非
/// "运行期依赖不可用"，因此统一在启动阶段 fail-fast：缺少或格式不对直接拒绝启动，
/// 而不是让数据库相关端点静默降级。
/// </summary>
public sealed class DeliveryKeyProvider
{
    public const string ConfigurationKey = "Security:DeliveryKey";

    private const int KeySize = 32;

    public DeliveryKeyProvider(string? configuredKey) => Key = Resolve(configuredKey);

    public byte[] Key { get; }

    /// <summary>启动期校验：缺失或格式不合法立刻抛异常，让宿主启动失败。</summary>
    public static void ValidateAtStartup(string? configuredKey) => _ = Resolve(configuredKey);

    private static byte[] Resolve(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "缺少配置项 " + ConfigurationKey + "（32 字节密钥的 Base64 字符串），无法解密远端连接串。" +
                "请执行 dotnet user-secrets --project ChatWithMe.Api set \"" + ConfigurationKey + "\" \"<密钥>\" 写入用户机密，" +
                "或设置环境变量 Security__DeliveryKey。");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(configured);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("配置项 " + ConfigurationKey + " 不是合法的 Base64 字符串。", exception);
        }

        if (key.Length != KeySize)
        {
            throw new InvalidOperationException(
                "配置项 " + ConfigurationKey + " 解码后为 " + key.Length + " 字节，AES-256-GCM 要求 32 字节。");
        }

        return key;
    }
}
