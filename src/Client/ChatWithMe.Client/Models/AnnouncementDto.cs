namespace ChatWithMe.Client.Models;

public sealed record AnnouncementDto(Guid Id, string Title, string Content, string CreatedAtUtc);
public sealed record AnnouncementPage(IReadOnlyList<AnnouncementDto> Items, int Total, int Page, int PageSize);
