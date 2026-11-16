using System.Globalization;

namespace ChatWithMe.Api.Serialization;

public static class TimeZoneResolution
{
    private static readonly string[] Candidates = ["Asia/Shanghai", "China Standard Time"];

    /// <summary>
    /// 解析 Time:TimeZoneId（IANA 或 Windows 时区 id 均可）。解析失败时依次尝试
    /// Time:FallbackTimeZoneId、Asia/Shanghai、China Standard Time，最后退回 UTC。
    /// </summary>
    public static TimeZoneInfo Resolve(IConfiguration configuration)
    {
        var configured = configuration["Time:TimeZoneId"];
        var fallback = configuration["Time:FallbackTimeZoneId"];

        var candidates = new List<string>(4);

        if (!string.IsNullOrWhiteSpace(configured))
        {
            candidates.Add(configured.Trim());
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            candidates.Add(fallback.Trim());
        }

        candidates.AddRange(Candidates);

        foreach (var candidate in candidates)
        {
            if (TryFind(candidate, out var timeZone))
            {
                return timeZone;
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static string Describe(TimeZoneInfo timeZone)
    {
        var offset = timeZone.BaseUtcOffset;
        var sign = offset < TimeSpan.Zero ? "-" : "+";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{timeZone.Id} (UTC{sign}{Math.Abs(offset.Hours):00}:{Math.Abs(offset.Minutes):00})");
    }

    private static bool TryFind(string id, out TimeZoneInfo timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            timeZone = TimeZoneInfo.Utc;
            return false;
        }
    }
}
