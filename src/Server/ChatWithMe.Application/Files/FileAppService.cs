using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Domain.Files;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Application.Files;

public sealed class FileAppService : IFileAppService
{
    private const string ContentPathPrefix = "/api/files/";

    private readonly IFileStorage _fileStorage;
    private readonly IFileAssetRepository _fileAssetRepository;
    private readonly IFileLinkSigner _fileLinkSigner;
    private readonly IClock _clock;
    private readonly FileUploadOptions _options;
    private readonly ILogger<FileAppService> _logger;

    public FileAppService(
        IFileStorage fileStorage,
        IFileAssetRepository fileAssetRepository,
        IFileLinkSigner fileLinkSigner,
        IClock clock,
        IOptions<FileUploadOptions> options,
        ILogger<FileAppService> logger)
    {
        _fileStorage = fileStorage;
        _fileAssetRepository = fileAssetRepository;
        _fileLinkSigner = fileLinkSigner;
        _clock = clock;
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

    public async Task<StoredFileContent?> OpenContentAsync(Guid id, CancellationToken cancellationToken)
    {
        var fileAsset = await _fileAssetRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (fileAsset is null)
        {
            return null;
        }

        return await _fileStorage
            .OpenReadAsync(fileAsset.RelativePath, fileAsset.ContentType, fileAsset.OriginalName, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<FileLinkDto?> CreateLinkAsync(Guid id, CancellationToken cancellationToken)
    {
        var fileAsset = await _fileAssetRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (fileAsset is null)
        {
            return null;
        }

        var lifetimeSeconds = _options.LinkLifetimeSeconds > 0 ? _options.LinkLifetimeSeconds : 300;
        var expiresAtUtc = _clock.UtcNow.AddSeconds(lifetimeSeconds);
        var signature = _fileLinkSigner.CreateSignature(fileAsset.Id, expiresAtUtc);
        var expiresAtUnix = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var url = ContentPathPrefix + fileAsset.Id + "/content?exp=" + expiresAtUnix + "&sig=" + Uri.EscapeDataString(signature);

        _logger.LogInformation("已生成文件回读链接：{FileAssetId}，{LifetimeSeconds} 秒后过期", fileAsset.Id, lifetimeSeconds);

        return new FileLinkDto(url, expiresAtUtc);
    }

    private static FileAssetDto ToDto(FileAsset fileAsset) =>
        new(
            fileAsset.Id,
            fileAsset.OriginalName,
            fileAsset.StoredName,
            fileAsset.RelativePath,
            fileAsset.ContentType,
            fileAsset.SizeInBytes,
            ContentPathPrefix + fileAsset.Id + "/content",
            fileAsset.CreatedAtUtc);
}
