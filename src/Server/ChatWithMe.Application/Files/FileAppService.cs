using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Application.Files;

public sealed class FileAppService : IFileAppService
{
    private readonly IFileStorage _fileStorage;
    private readonly IFileAssetRepository _fileAssetRepository;
    private readonly FileUploadOptions _options;
    private readonly ILogger<FileAppService> _logger;

    public FileAppService(
        IFileStorage fileStorage,
        IFileAssetRepository fileAssetRepository,
        IOptions<FileUploadOptions> options,
        ILogger<FileAppService> logger)
    {
        _fileStorage = fileStorage;
        _fileAssetRepository = fileAssetRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FileAssetDto> UploadAsync(Stream content, string originalFileName, string contentType, long lengthInBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (lengthInBytes <= 0)
        {
            throw new FileValidationException("上传的文件为空。");
        }

        if (lengthInBytes > _options.MaxFileSizeBytes)
        {
            throw new FileTooLargeException("文件大小 " + lengthInBytes + " 字节超过上限 " + _options.MaxFileSizeBytes + " 字节。");
        }

        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new FileValidationException("文件缺少扩展名。");
        }

        if (extension.Contains("..", StringComparison.Ordinal) || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new FileValidationException("文件扩展名 " + extension + " 非法。");
        }

        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new FileValidationException("不支持的文件类型 " + extension + "。");
        }

        var storedFile = await _fileStorage.SaveAsync(content, originalFileName, contentType, cancellationToken).ConfigureAwait(false);

        if (storedFile.SizeInBytes > _options.MaxFileSizeBytes)
        {
            await _fileStorage.DeleteAsync(storedFile.RelativePath, cancellationToken).ConfigureAwait(false);
            throw new FileTooLargeException("文件大小 " + storedFile.SizeInBytes + " 字节超过上限 " + _options.MaxFileSizeBytes + " 字节。");
        }

        var fileAsset = FileAsset.Create(originalFileName, storedFile.StoredName, storedFile.RelativePath, contentType, storedFile.SizeInBytes);

        await _fileAssetRepository.AddAsync(fileAsset, cancellationToken).ConfigureAwait(false);
        await _fileAssetRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("文件上传完成：{FileAssetId} {StoredName} {SizeInBytes} 字节", fileAsset.Id, fileAsset.StoredName, fileAsset.SizeInBytes);

        return ToDto(fileAsset);
    }

    public async Task<FileAssetDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var fileAsset = await _fileAssetRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return fileAsset is null ? null : ToDto(fileAsset);
    }

    public async Task<PagedResult<FileAssetDto>> ListAsync(int page, int pageSize, string? keyword, CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedPageSize, skip) = Paging.Normalize(page, pageSize);
        var normalizedKeyword = Paging.NormalizeKeyword(keyword);

        var total = await _fileAssetRepository.CountAsync(normalizedKeyword, cancellationToken).ConfigureAwait(false);
        var fileAssets = total == 0
            ? Array.Empty<FileAsset>()
            : await _fileAssetRepository.ListAsync(skip, normalizedPageSize, normalizedKeyword, cancellationToken).ConfigureAwait(false);

        return new PagedResult<FileAssetDto>(fileAssets.Select(ToDto).ToList(), total, normalizedPage, normalizedPageSize);
    }

    private FileAssetDto ToDto(FileAsset fileAsset)
    {
        var prefix = string.IsNullOrWhiteSpace(_options.PublicUrlPrefix) ? "/uploads" : _options.PublicUrlPrefix.TrimEnd('/');

        return new FileAssetDto(
            fileAsset.Id,
            fileAsset.OriginalName,
            fileAsset.StoredName,
            fileAsset.RelativePath,
            fileAsset.ContentType,
            fileAsset.SizeInBytes,
            prefix + "/" + fileAsset.RelativePath,
            fileAsset.CreatedAtUtc);
    }
}
