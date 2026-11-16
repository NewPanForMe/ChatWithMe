namespace ChatWithMe.Infrastructure.Common;

/// <summary>
/// 为 ILIKE 关键字构造安全的 LIKE 模式：转义 LIKE 的元字符，避免用户输入的 % 和 _ 变成通配符。
/// </summary>
internal static class LikePatterns
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string keyword)
    {
        var escaped = keyword
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return "%" + escaped + "%";
    }
}
