using System.Text.RegularExpressions;

namespace ChatWithMe.Infrastructure.ConnectionStrings;

public static partial class ConnectionStringMasker
{
    [GeneratedRegex("(?<key>(?:password|pwd)\\s*=\\s*)(?<value>[^;]*)", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordPattern();

    public static string Mask(string? connectionString) =>
        string.IsNullOrWhiteSpace(connectionString)
            ? string.Empty
            : PasswordPattern().Replace(connectionString, match => match.Groups["key"].Value + "***");
}
