using ChatWithMe.Application.Common;

namespace ChatWithMe.Application.Files;

public interface IFileAppService
{
    Task<FileAssetDto> UploadAsync(Stream content, string originalFileName, string contentType, long lengthInBytes, CancellationToken cancellationToken);

    Task<FileAssetDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<FileAssetDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken);
}
