using ChatWithMe.Application.Common;

namespace ChatWithMe.Tests;

public sealed class PagingTests
{
    [Theory]
    [InlineData(0, 0, 1, 20, 0)]
    [InlineData(-5, -1, 1, 20, 0)]
    [InlineData(2, 10, 2, 10, 10)]
    [InlineData(3, 1000, 3, 100, 200)]
    public void Normalize_ClampsToValidRange(int page, int pageSize, int expectedPage, int expectedPageSize, int expectedSkip)
    {
        var (normalizedPage, normalizedPageSize, skip) = Paging.Normalize(page, pageSize);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedPageSize, normalizedPageSize);
        Assert.Equal(expectedSkip, skip);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(" abc ", "abc")]
    public void NormalizeKeyword_TrimsAndDropsEmpty(string? input, string? expected)
    {
        Assert.Equal(expected, Paging.NormalizeKeyword(input));
    }
}
