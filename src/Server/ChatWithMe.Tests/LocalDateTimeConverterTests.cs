using System.Text.Json;
using ChatWithMe.Api.Serialization;

namespace ChatWithMe.Tests;

public sealed class LocalDateTimeConverterTests
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new LocalDateTimeConverter(TimeZone));

        return options;
    }

    [Fact]
    public void Serialize_UtcValue_RendersShanghaiWallClockWithoutSuffix()
    {
        var options = CreateOptions();
        var value = new DateTime(2026, 11, 16, 8, 38, 48, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(value, options);

        Assert.Equal("\"2026/11/16 16:38:48\"", json);
    }

    [Fact]
    public void Deserialize_DisplayFormat_ReturnsSameInstantAsUtc()
    {
        var options = CreateOptions();

        var value = JsonSerializer.Deserialize<DateTime>("\"2026/11/16 16:38:48\"", options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 11, 16, 8, 38, 48, DateTimeKind.Utc), value);
    }

    [Fact]
    public void Cursor_RoundTripsWithMillisecondPrecision()
    {
        var utc = new DateTime(2026, 11, 16, 8, 39, 6, 382, DateTimeKind.Utc);

        var cursor = LocalDateTimeConverter.FormatCursor(utc, TimeZone);

        Assert.Equal("2026/11/16 16:39:06.382", cursor);
        Assert.True(LocalDateTimeConverter.TryParse(cursor, TimeZone, out var parsed));
        Assert.Equal(utc, parsed);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_InvalidValues_ReturnFalse(string? text)
    {
        Assert.False(LocalDateTimeConverter.TryParse(text, TimeZone, out _));
    }

    [Fact]
    public void NullableDateTime_IsConvertedToo()
    {
        var options = CreateOptions();
        DateTime? value = new DateTime(2026, 11, 16, 8, 38, 48, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(value, options);

        Assert.Equal("\"2026/11/16 16:38:48\"", json);
    }
}
