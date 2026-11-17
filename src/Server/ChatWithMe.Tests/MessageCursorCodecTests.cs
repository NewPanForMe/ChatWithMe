using ChatWithMe.Api.Serialization;
using ChatWithMe.Domain.Messages;

namespace ChatWithMe.Tests;

public sealed class MessageCursorCodecTests
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");

    [Fact]
    public void EncodeAndDecode_RoundTripsTimestampAndMessageId()
    {
        var sentAtUtc = new DateTime(2026, 11, 17, 1, 2, 3, 456, DateTimeKind.Utc);
        var cursor = new MessageCursor(sentAtUtc, Guid.NewGuid());

        var encoded = MessageCursorCodec.Encode(cursor, TimeZone);

        Assert.StartsWith("2026/11/17 09:02:03.456|", encoded);
        Assert.True(MessageCursorCodec.TryDecode(encoded, TimeZone, out var decoded));
        Assert.Equal(cursor, decoded);
    }

    [Fact]
    public void Decode_LegacyTimestampOnlyCursor_ProducesCursorWithoutMessageId()
    {
        Assert.True(MessageCursorCodec.TryDecode("2026/11/17 09:02:03.456", TimeZone, out var decoded));

        Assert.NotNull(decoded);
        Assert.Equal(new DateTime(2026, 11, 17, 1, 2, 3, 456, DateTimeKind.Utc), decoded!.SentAtUtc);
        Assert.Null(decoded.MessageId);
    }

    [Fact]
    public void Encode_TimestampOnlyCursor_OmitsSeparator()
    {
        var encoded = MessageCursorCodec.Encode(
            new MessageCursor(new DateTime(2026, 11, 17, 1, 2, 3, 456, DateTimeKind.Utc)),
            TimeZone);

        Assert.Equal("2026/11/17 09:02:03.456", encoded);
        Assert.DoesNotContain(MessageCursorCodec.Separator, encoded);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026/11/17 09:02:03.456|not-a-guid")]
    [InlineData("2026/11/17 09:02:03.456|")]
    [InlineData("")]
    [InlineData(null)]
    public void Decode_InvalidValues_ReturnFalse(string? text)
    {
        Assert.False(MessageCursorCodec.TryDecode(text, TimeZone, out var decoded));
        Assert.Null(decoded);
    }
}
