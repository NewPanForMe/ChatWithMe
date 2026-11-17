namespace ChatWithMe.Application.Abstractions;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);

    /// <summary>打开已存储的文件用于鉴权回读；文件不存在时抛 <see cref="Common.NotFoundException"/>。</summary>
    Task<StoredFileContent> OpenReadAsync(string relativePath, string contentType, string fileName, CancellationToken cancellationToken);
}
