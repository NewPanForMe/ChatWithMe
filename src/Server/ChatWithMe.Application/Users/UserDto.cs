namespace ChatWithMe.Application.Users;

public sealed record UserDto(Guid Id, string Email, string NickName, DateTime CreatedAtUtc);
