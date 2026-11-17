using ChatWithMe.Infrastructure.ConnectionStrings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatWithMe.Tests;

public sealed class ConnectionStringCacheTests
{
    private static readonly byte[] Key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);

    private static ConnectionStringCache CreateCache(string directory) =>
        new(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [ConnectionStringCache.DirectoryConfigurationKey] = directory,
                })
                .Build(),
            NullLogger<ConnectionStringCache>.Instance);

    [Fact]
    public void WriteThenRead_RoundTripsEncryptedConnectionString()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cwm-cache-" + Guid.NewGuid().ToString("N"));
        var cache = CreateCache(directory);

        try
        {
            cache.Write("ChatWithMe", "Host=db;Username=u;Password=secret", Key);

            var cached = cache.TryRead("ChatWithMe", Key);

            Assert.NotNull(cached);
            Assert.Equal("Host=db;Username=u;Password=secret", cached!.ConnectionString);
            Assert.True(cached.CachedAtUtc <= DateTime.UtcNow.AddMinutes(1));

            var raw = File.ReadAllText(Path.Combine(directory, "ChatWithMe.cache"));
            Assert.DoesNotContain("secret", raw, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void TryRead_WithWrongKey_ReturnsNull()
    {
        var directory = Path.Combine(Path.GetTempPath(), "cwm-cache-" + Guid.NewGuid().ToString("N"));
        var cache = CreateCache(directory);

        try
        {
            cache.Write("ChatWithMe", "Host=db", Key);

            Assert.Null(cache.TryRead("ChatWithMe", System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void TryRead_MissingFile_ReturnsNull()
    {
        var cache = CreateCache(Path.Combine(Path.GetTempPath(), "cwm-cache-" + Guid.NewGuid().ToString("N")));

        Assert.Null(cache.TryRead("Missing", Key));
    }
}

public sealed class DeliveryKeyProviderTests
{
    [Fact]
    public void Constructor_ValidBase64Key_ExposesBytes()
    {
        var key = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var provider = new DeliveryKeyProvider(Convert.ToBase64String(key));

        Assert.Equal(key, provider.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64")]
    public void Constructor_MissingOrMalformedKey_Throws(string? configured)
    {
        Assert.Throws<InvalidOperationException>(() => new DeliveryKeyProvider(configured));
    }

    [Fact]
    public void Constructor_KeyWithWrongLength_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => new DeliveryKeyProvider(Convert.ToBase64String(new byte[16])));

        Assert.Contains("32 字节", exception.Message);
    }
}
