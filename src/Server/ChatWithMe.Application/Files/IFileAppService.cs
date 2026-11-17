using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;

namespace ChatWithMe.Application.Files;

public interface IFileAppService
{
    Task<FileAssetDto> UploadAsync(Stream content, string originalFileName, string contentType, long lengthInBytes, CancellationToken cancellationToken);

    Task<FileAssetDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<FileAssetDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken);

    /// <summary>按元数据打开文件内容用于鉴权回读；文件不存在返回 null。</summary>
    Task<StoredFileContent?> OpenContentAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>生成短时签名回读链接；文件不存在返回 null。</summary>
    Task<FileLinkDto?> CreateLinkAsync(Guid id, CancellationToken cancellationToken);
}
