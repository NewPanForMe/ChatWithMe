namespace ChatWithMe.Application.Common;

public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message)
    {
    }
}

public sealed class FileTooLargeException : FileValidationException
{
    public FileTooLargeException(string message) : base(message)
    {
    }
}
