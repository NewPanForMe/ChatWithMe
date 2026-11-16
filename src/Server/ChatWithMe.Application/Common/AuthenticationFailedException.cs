namespace ChatWithMe.Application.Common;

public abstract class AuthenticationFailedException : Exception
{
    protected AuthenticationFailedException(string message) : base(message)
    {
    }
}

public sealed class InvalidCredentialsException : AuthenticationFailedException
{
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}

public sealed class RefreshTokenRejectedException : AuthenticationFailedException
{
    public RefreshTokenRejectedException(string message) : base(message)
    {
    }
}
