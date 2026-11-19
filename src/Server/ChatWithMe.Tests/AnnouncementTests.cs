using ChatWithMe.Domain.Notifications;

namespace ChatWithMe.Tests;

public sealed class AnnouncementTests
{
    [Fact]
    public void Create_TrimsTextAndPreservesAuthor()
    {
        var author = Guid.NewGuid();
        var item = Announcement.Create(author, " 标题 ", " 正文\n第二行 ", DateTime.UtcNow);
        Assert.Equal(author, item.AdminId);
        Assert.Equal("标题", item.Title);
        Assert.Equal("正文\n第二行", item.Content);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Theory]
    [InlineData("", "内容")]
    [InlineData("标题", " ")]
    public void Create_RejectsEmptyText(string title, string content) =>
        Assert.Throws<ArgumentException>(() => Announcement.Create(Guid.NewGuid(), title, content, DateTime.UtcNow));

    [Fact]
    public void Create_EnforcesLengths()
    {
        Assert.Throws<ArgumentException>(() => Announcement.Create(Guid.NewGuid(), new string('a', 101), "内容", DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() => Announcement.Create(Guid.NewGuid(), "标题", new string('a', 10001), DateTime.UtcNow));
        Assert.Equal(10000, Announcement.Create(Guid.NewGuid(), new string('a', 100), new string('a', 10000), DateTime.UtcNow).Content.Length);
    }
}
