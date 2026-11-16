namespace ChatWithMe.Application.Abstractions;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
