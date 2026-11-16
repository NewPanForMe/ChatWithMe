namespace ChatWithMe.Application.Common;

public static class Paging
{
    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    /// <summary>
    /// 把外部传入的分页参数收敛到合法范围，并换算出 Skip。
    /// </summary>
    public static (int Page, int PageSize, int Skip) Normalize(int page, int pageSize)
    {
        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        return (normalizedPage, normalizedPageSize, (normalizedPage - 1) * normalizedPageSize);
    }

    public static string? NormalizeKeyword(string? keyword) =>
        string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
}
