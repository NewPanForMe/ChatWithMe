using ChatWithMe.Client.Models;

namespace ChatWithMe.Client.Services;

/// <summary>当前登录用户（进程内），界面绑定它显示头像和昵称。</summary>
public sealed class Session
{
    public UserDto? User { get; private set; }

    public bool IsAuthenticated => User is not null;

    public Guid UserId => User?.Id ?? Guid.Empty;

    public string DisplayName => User?.NickName ?? "未登录";

    public void SetUser(UserDto user) => User = user;

    public void Clear() => User = null;
}
