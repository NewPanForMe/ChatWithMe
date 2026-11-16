using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Infrastructure.ConnectionStrings;

public sealed class RuntimeConnectionStringProvider : IConnectionStringProvider
{
    public const string HttpClientName = "ChatWithMe.ConnectionString";

    public const string DeliveryKeyConfigurationKey = "Security:DeliveryKey";

    public const string EndpointConfigurationKey = "Security:ConnectionStringEndpoint";

    public const string PostgresConnectionNameConfigurationKey = "Security:ConnectionNames:Postgres";

    private const string DefaultEndpoint = "https://www.tunn.work/ToolsConsole/ConnectionString/GetByConnName?connName=ChatWithMe";
    private const string DefaultConnectionName = "ChatWithMe";
    private const string ConnectionNameQueryKey = "connName=";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int PayloadOverhead = NonceSize + TagSize;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RuntimeConnectionStringProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeConnectionStringProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<RuntimeConnectionStringProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public string GetConnectionString(string? connName = null)
    {
        var resolvedName = ResolveConnectionName(connName);

        if (_cache.TryGetValue(resolvedName, out var cached))
        {
            return cached;
        }

        _gate.Wait();

        try
        {
            if (_cache.TryGetValue(resolvedName, out cached))
            {
                return cached;
            }

            var connectionString = ResolveAsync(resolvedName, CancellationToken.None).GetAwaiter().GetResult();
            _cache[resolvedName] = connectionString;

            return connectionString;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> GetConnectionStringAsync(string? connName = null, CancellationToken cancellationToken = default)
    {
        var resolvedName = ResolveConnectionName(connName);

        if (_cache.TryGetValue(resolvedName, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_cache.TryGetValue(resolvedName, out cached))
            {
                return cached;
            }

            var connectionString = await ResolveAsync(resolvedName, cancellationToken).ConfigureAwait(false);
            _cache[resolvedName] = connectionString;

            return connectionString;
        }
        finally
        {
            _gate.Release();
        }
    }

    private string ResolveConnectionName(string? connName)
    {
        if (!string.IsNullOrWhiteSpace(connName))
        {
            return connName.Trim();
        }

        var configured = _configuration[PostgresConnectionNameConfigurationKey];

        return string.IsNullOrWhiteSpace(configured) ? DefaultConnectionName : configured.Trim();
    }

    private async Task<string> ResolveAsync(string connName, CancellationToken cancellationToken)
    {
        var key = ResolveDeliveryKey();
        var endpoint = _configuration[EndpointConfigurationKey];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = DefaultEndpoint;
        }

        var payload = await RequestPayloadAsync(WithConnectionName(endpoint, connName), cancellationToken).ConfigureAwait(false);

        return Decrypt(payload.ConnectionStringEncrypted!, key);
    }

    private byte[] ResolveDeliveryKey()
    {
        var configured = _configuration[DeliveryKeyConfigurationKey];

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "缺少配置项 Security:DeliveryKey（32 字节密钥的 Base64 字符串），无法解密远端连接串。" +
                "请执行 dotnet user-secrets --project ChatWithMe.Api set \"Security:DeliveryKey\" \"<密钥>\" 写入用户机密，或设置环境变量 Security__DeliveryKey。");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(configured);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("配置项 Security:DeliveryKey 不是合法的 Base64 字符串。", ex);
        }

        if (key.Length != KeySize)
        {
            throw new InvalidOperationException(
                "配置项 Security:DeliveryKey 解码后为 " + key.Length + " 字节，AES-256-GCM 要求 32 字节。");
        }

        return key;
    }

    private static string WithConnectionName(string endpoint, string connName)
    {
        var index = endpoint.IndexOf(ConnectionNameQueryKey, StringComparison.OrdinalIgnoreCase);

        if (index < 0)
        {
            return endpoint + (endpoint.Contains('?', StringComparison.Ordinal) ? "&" : "?") + ConnectionNameQueryKey + Uri.EscapeDataString(connName);
        }

        var valueStart = index + ConnectionNameQueryKey.Length;
        var valueEnd = endpoint.IndexOf('&', valueStart);

        if (valueEnd < 0)
        {
            valueEnd = endpoint.Length;
        }

        return string.Concat(endpoint.AsSpan(0, valueStart), Uri.EscapeDataString(connName), endpoint.AsSpan(valueEnd));
    }

    private async Task<ConnectionStringPayload> RequestPayloadAsync(string endpoint, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            using var response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var envelope = await response.Content
                .ReadFromJsonAsync<ConnectionStringEnvelope>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            if (envelope is null)
            {
                throw new InvalidOperationException("远端连接串接口返回了空响应。");
            }

            if (envelope.Code != 0 || envelope.Data is null)
            {
                throw new InvalidOperationException(
                    "远端连接串接口返回失败：code=" + envelope.Code + "，message=" + envelope.Message);
            }

            if (string.IsNullOrWhiteSpace(envelope.Data.ConnectionStringEncrypted))
            {
                throw new InvalidOperationException("远端连接串接口未返回 connectionStringEncrypted，无法解密。");
            }

            if (!string.IsNullOrWhiteSpace(envelope.Data.Algorithm) &&
                !string.Equals(envelope.Data.Algorithm, "AES-256-GCM", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "远端连接串接口声明的算法是 " + envelope.Data.Algorithm + "，与实现支持的 AES-256-GCM 不一致。");
            }

            return envelope.Data;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            throw new InvalidOperationException("从远端接口获取连接串失败（" + endpoint + "）：" + ex.Message, ex);
        }
    }

    private string Decrypt(string encryptedPayload, byte[] key)
    {
        try
        {
            var raw = Convert.FromBase64String(encryptedPayload);

            if (raw.Length <= PayloadOverhead)
            {
                throw new CryptographicException(
                    "载荷长度 " + raw.Length + " 字节不足，至少需要 " + (PayloadOverhead + 1) + " 字节。");
            }

            var nonce = raw.AsSpan(0, NonceSize);
            var tag = raw.AsSpan(raw.Length - TagSize, TagSize);
            var ciphertext = raw.AsSpan(NonceSize, raw.Length - PayloadOverhead);
            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(key, TagSize))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            var connectionString = Encoding.UTF8.GetString(plaintext);
            CryptographicOperations.ZeroMemory(plaintext);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new CryptographicException("解密结果为空。");
            }

            _logger.LogInformation("远端连接串获取成功（已脱敏）：{ConnectionString}", ConnectionStringMasker.Mask(connectionString));

            return connectionString;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new InvalidOperationException(
                "连接串解密失败（AES-256-GCM）：请确认 Security:DeliveryKey 与服务端一致，且载荷为 Base64(nonce[12] + ciphertext + tag[16])。原因：" + ex.Message,
                ex);
        }
    }
}
