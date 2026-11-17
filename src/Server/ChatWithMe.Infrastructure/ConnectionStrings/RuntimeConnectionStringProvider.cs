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

    public const string EndpointConfigurationKey = "Security:ConnectionStringEndpoint";

    public const string PostgresConnectionNameConfigurationKey = "Security:ConnectionNames:Postgres";

    private const string DefaultEndpoint = "https://www.tunn.work/ToolsConsole/ConnectionString/GetByConnName?connName=ChatWithMe";
    private const string DefaultConnectionName = "ChatWithMe";
    private const string ConnectionNameQueryKey = "connName=";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int PayloadOverhead = NonceSize + TagSize;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly DeliveryKeyProvider _deliveryKey;
    private readonly ConnectionStringCache _cacheStore;
    private readonly ILogger<RuntimeConnectionStringProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<string, string> _resolved = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConnectionStringStatus> _statuses = new(StringComparer.OrdinalIgnoreCase);

    public RuntimeConnectionStringProvider(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        DeliveryKeyProvider deliveryKey,
        ConnectionStringCache cacheStore,
        ILogger<RuntimeConnectionStringProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _deliveryKey = deliveryKey;
        _cacheStore = cacheStore;
        _logger = logger;
    }

    public string GetConnectionString(string? connName = null)
    {
        var resolvedName = ResolveConnectionName(connName);

        if (_resolved.TryGetValue(resolvedName, out var cached))
        {
            return cached;
        }

        _gate.Wait();

        try
        {
            if (_resolved.TryGetValue(resolvedName, out cached))
            {
                return cached;
            }

            var connectionString = ResolveAsync(resolvedName, CancellationToken.None).GetAwaiter().GetResult();
            _resolved[resolvedName] = connectionString;

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

        if (_resolved.TryGetValue(resolvedName, out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_resolved.TryGetValue(resolvedName, out cached))
            {
                return cached;
            }

            var connectionString = await ResolveAsync(resolvedName, cancellationToken).ConfigureAwait(false);
            _resolved[resolvedName] = connectionString;

            return connectionString;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>最近一次解析状态；健康检查与启动日志据此显式报告"远端 / 缓存"。</summary>
    public ConnectionStringStatus GetStatus(string? connName = null)
    {
        var resolvedName = ResolveConnectionName(connName);

        return _statuses.TryGetValue(resolvedName, out var status)
            ? status
            : ConnectionStringStatus.Unknown(resolvedName);
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
        var key = _deliveryKey.Key;
        var endpoint = _configuration[EndpointConfigurationKey];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = DefaultEndpoint;
        }

        try
        {
            var payload = await RequestPayloadAsync(WithConnectionName(endpoint, connName), cancellationToken).ConfigureAwait(false);
            var connectionString = Decrypt(payload.ConnectionStringEncrypted!, key);

            _cacheStore.Write(connName, connectionString, key);
            _statuses[connName] = new ConnectionStringStatus(
                connName,
                ConnectionStringSource.Remote,
                DateTime.UtcNow,
                null,
                endpoint,
                "连接串来自远端 ToolsConsole");

            return connectionString;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var cached = _cacheStore.TryRead(connName, key);

            if (cached is null)
            {
                _statuses[connName] = new ConnectionStringStatus(
                    connName,
                    ConnectionStringSource.Unknown,
                    DateTime.UtcNow,
                    null,
                    endpoint,
                    "远端连接串不可用且本地无可用缓存：" + exception.Message);

                throw new InvalidOperationException(
                    "从远端接口获取连接串失败，且本地没有可用缓存（" + _cacheStore.CacheDirectory + "）：" + exception.Message,
                    exception);
            }

            _logger.LogWarning(
                exception,
                "远端连接串不可用，回退到本地加密缓存（缓存时间 {CachedAtUtc}）：{Reason}",
                cached.CachedAtUtc,
                exception.Message);

            _statuses[connName] = new ConnectionStringStatus(
                connName,
                ConnectionStringSource.Cache,
                DateTime.UtcNow,
                cached.CachedAtUtc,
                endpoint,
                "远端连接串不可用，已回退本地缓存：" + exception.Message);

            return cached.ConnectionString;
        }
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
