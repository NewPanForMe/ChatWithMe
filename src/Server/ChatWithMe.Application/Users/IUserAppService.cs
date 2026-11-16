using ChatWithMe.Application.Common;

namespace ChatWithMe.Application.Users;

public interface IUserAppService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken);
}
