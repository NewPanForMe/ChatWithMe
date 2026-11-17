using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Infrastructure.ConnectionStrings;

/// <summary>
/// 最近一次成功连接串的本地加密缓存。远端 ToolsConsole 不可达时用它兜底，
/// 避免启动期单点；缓存用同一个 <c>Security:DeliveryKey</c>（AES-256-GCM）加密，
/// 不落明文。文件格式：首行缓存时间（ISO-8601 UTC），次行 Base64(nonce[12] + ciphertext + tag[16])。
/// </summary>
public sealed class ConnectionStringCache
{
    public const string DirectoryConfigurationKey = "Security:ConnectionStringCacheDirectory";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int PayloadOverhead = NonceSize + TagSize;

    private readonly string _directory;
    private readonly ILogger<ConnectionStringCache> _logger;

    public ConnectionStringCache(IConfiguration configuration, ILogger<ConnectionStringCache> logger)
    {
        _logger = logger;

        var configuredDirectory = configuration[DirectoryConfigurationKey];
        var root = string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "connection-string-cache")
            : Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(AppContext.BaseDirectory, configuredDirectory);

        _directory = Path.GetFullPath(root);
    }

    public string CacheDirectory => _directory;

    public CachedConnectionString? TryRead(string connectionName, byte[] key)
    {
        var path = PathFor(connectionName);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var lines = File.ReadAllLines(path);

            if (lines.Length < 2 || !DateTime.TryParse(lines[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var cachedAtUtc))
            {
                return null;
            }

            var payload = Convert.FromBase64String(lines[1]);
            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(payload.Length - TagSize, TagSize);
            var ciphertext = payload.AsSpan(NonceSize, payload.Length - PayloadOverhead);
            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext, Encoding.UTF8.GetBytes(AadFor(connectionName)));
            }

            var connectionString = Encoding.UTF8.GetString(plaintext);
            CryptographicOperations.ZeroMemory(plaintext);

            return string.IsNullOrWhiteSpace(connectionString)
                ? null
                : new CachedConnectionString(connectionString, DateTime.SpecifyKind(cachedAtUtc, DateTimeKind.Utc));
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "本地连接串缓存无法读取（{Path}），忽略该缓存：{Reason}", path, exception.Message);

            return null;
        }
    }

    public void Write(string connectionName, string connectionString, byte[] key)
    {
        try
        {
            Directory.CreateDirectory(_directory);

            var plaintext = Encoding.UTF8.GetBytes(connectionString);
            var payload = new byte[NonceSize + plaintext.Length + TagSize];
            var nonce = payload.AsSpan(0, NonceSize);
            var ciphertext = payload.AsSpan(NonceSize, plaintext.Length);
            var tag = payload.AsSpan(payload.Length - TagSize, TagSize);

            RandomNumberGenerator.Fill(nonce);

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes(AadFor(connectionName)));
            }

            CryptographicOperations.ZeroMemory(plaintext);

            var path = PathFor(connectionName);
            var content = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) + Environment.NewLine +
                          Convert.ToBase64String(payload) + Environment.NewLine;

            File.WriteAllText(path, content);

            _logger.LogInformation("已更新本地连接串加密缓存：{Path}", path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CryptographicException)
        {
            // 缓存写失败不影响本次解析结果，只是失去下次兜底能力。
            _logger.LogWarning(exception, "写入本地连接串缓存失败（{Directory}）：{Reason}", _directory, exception.Message);
        }
    }

    private string PathFor(string connectionName) =>
        Path.Combine(_directory, SanitizeFileName(connectionName) + ".cache");

    private static string AadFor(string connectionName) => "cwm-connection-string:" + connectionName;

    private static string SanitizeFileName(string connectionName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(connectionName.Length);

        foreach (var character in connectionName)
        {
            builder.Append(invalid.Contains(character) ? '_' : character);
        }

        return builder.ToString();
    }
}

public sealed record CachedConnectionString(string ConnectionString, DateTime CachedAtUtc);
