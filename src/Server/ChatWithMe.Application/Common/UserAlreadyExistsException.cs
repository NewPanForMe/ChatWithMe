namespace ChatWithMe.Application.Common;

public sealed class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string message) : base(message)
    {
    }
}
