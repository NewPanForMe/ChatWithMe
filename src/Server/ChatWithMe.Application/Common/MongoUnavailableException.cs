namespace ChatWithMe.Application.Common;

public sealed class MongoUnavailableException : Exception
{
    public MongoUnavailableException(string message) : base(message)
    {
    }

    public MongoUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
