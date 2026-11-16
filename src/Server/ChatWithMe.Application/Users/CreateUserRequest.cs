namespace ChatWithMe.Application.Users;

public sealed record CreateUserRequest(string Email, string NickName, string Password);
