using ChatWithMe.Application.Common;

namespace ChatWithMe.Application.Users;

public interface IUserAppService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken);

    /// <summary>删除聊天用户（管理端专用），同时清理会话成员、刷新令牌、好友与拉黑关系；消息保留。</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
