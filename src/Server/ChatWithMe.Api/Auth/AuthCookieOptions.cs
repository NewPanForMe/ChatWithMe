namespace ChatWithMe.Api.Auth;

/// <summary>
/// 刷新令牌 Cookie 的写入参数。刷新令牌只走 HttpOnly Cookie，前端 JS 读不到，
/// 因此同源 XSS 无法把它带走；access token 只留在内存，不落 localStorage。
/// </summary>
public sealed class AuthCookieOptions
{
    public const string SectionName = "Auth:Cookie";

    public string Name { get; set; } = "cwm_refresh";

    /// <summary>只在鉴权端点回传，避免刷新令牌随普通接口请求发出去。</summary>
    public string Path { get; set; } = "/api/auth";

    /// <summary>HTTPS 部署必须为 true；本机 http 调试可在 appsettings.Development.json 里关掉。</summary>
    public bool Secure { get; set; } = true;

    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    public string? Domain { get; set; }
}
