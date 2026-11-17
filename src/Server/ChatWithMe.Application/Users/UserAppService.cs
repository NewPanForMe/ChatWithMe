using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Logging;

namespace ChatWithMe.Application.Users;

public sealed class UserAppService : IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserAppService> _logger;

    public UserAppService(IUserRepository userRepository, ILogger<UserAppService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = Email.Create(request.Email);

        if (await _userRepository.ExistsWithEmailAsync(email.Value, cancellationToken).ConfigureAwait(false))
        {
            throw new UserAlreadyExistsException("邮箱 " + email.Value + " 已被注册。");
        }

        var user = User.Register(email.Value, request.NickName, PasswordHasher.Hash(request.Password));

        await _userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _userRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var domainEvent in user.DomainEvents.OfType<UserRegisteredDomainEvent>())
        {
            _logger.LogInformation("领域事件 UserRegisteredDomainEvent：{UserId} {Email} {OccurredOnUtc}", domainEvent.UserId, domainEvent.Email, domainEvent.OccurredOnUtc);
        }

        user.ClearDomainEvents();

        return ToDto(user);
    }

    public async Task<UserDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return user is null ? null : ToDto(user);
    }

    public async Task<PagedResult<UserDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize, skip) = Paging.Normalize(page, pageSize);
        var normalizedKeyword = Paging.NormalizeKeyword(keyword);

        var total = await _userRepository.CountAsync(normalizedKeyword, cancellationToken).ConfigureAwait(false);
        var users = total == 0
            ? Array.Empty<User>()
            : await _userRepository.ListAsync(skip, normalizedPageSize, normalizedKeyword, cancellationToken).ConfigureAwait(false);

        return new PagedResult<UserDto>(users.Select(ToDto).ToList(), total, normalizedPage, normalizedPageSize);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("用户不存在。");

        await _userRepository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);

        _logger.LogWarning("用户已删除：{UserId} {Email}", user.Id, user.Email.Value);
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email.Value, user.NickName, user.CreatedAtUtc);
}
