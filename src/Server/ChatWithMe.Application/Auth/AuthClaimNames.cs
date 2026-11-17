namespace ChatWithMe.Application.Auth;

/// <summary>访问令牌中的自定义声明名，API 与基础设施共用，避免字符串散落。</summary>
public static class AuthClaimNames
{
    public const string Role = "role";

    public const string SessionVersion = "sv";

    /// <summary>主体类型：区分管理后台账号与聊天用户。</summary>
    public const string PrincipalType = "typ";

    public const string PrincipalUser = "user";

    public const string PrincipalAdmin = "admin";
}
