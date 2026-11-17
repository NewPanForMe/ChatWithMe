using System.Globalization;
using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Common;
using ChatWithMe.Application.Files;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChatWithMe.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IOptions<FileUploadOptions> options, IHostEnvironment environment, ILogger<LocalFileStorage> logger)
    {
        var configuredRoot = options.Value.StorageRoot;

        _root = Path.GetFullPath(
            Path.IsPathRooted(configuredRoot)
                ? configuredRoot
                : Path.Combine(environment.ContentRootPath, configuredRoot));

        Directory.CreateDirectory(_root);

        _logger = logger;
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var extension = Path.GetExtension(originalFileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new FileValidationException("文件缺少扩展名，无法保存。");
        }

        if (extension.Contains("..", StringComparison.Ordinal) || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new FileValidationException("文件扩展名 " + extension + " 非法。");
        }

        extension = extension.ToLowerInvariant();

        var now = DateTime.UtcNow;
        var relativeDirectory = Path.Combine(
            now.ToString("yyyy", CultureInfo.InvariantCulture),
            now.ToString("MM", CultureInfo.InvariantCulture));

        var storedName = Guid.NewGuid().ToString("N") + extension;
        var directory = Path.Combine(_root, relativeDirectory);
        Directory.CreateDirectory(directory);

        var absolutePath = Path.Combine(directory, storedName);
        long sizeInBytes;

        await using (var target = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
            sizeInBytes = target.Length;
        }

        if (sizeInBytes <= 0)
        {
            File.Delete(absolutePath);
            throw new FileValidationException("上传的文件为空。");
        }

        var relativePath = (relativeDirectory + "/" + storedName).Replace('\\', '/');

        _logger.LogInformation("文件已写入 {RelativePath}（{SizeInBytes} 字节）", relativePath, sizeInBytes);

        return new StoredFile(storedName, relativePath, sizeInBytes);
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var absolutePath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileValidationException("非法文件路径：" + relativePath);
        }

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogInformation("已删除文件 {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }

    public Task<StoredFileContent> OpenReadAsync(string relativePath, string contentType, string fileName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var absolutePath = Path.GetFullPath(Path.Combine(_root, relativePath));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new FileValidationException("非法文件路径：" + relativePath);
        }

        if (!File.Exists(absolutePath))
        {
            throw new NotFoundException("文件已不在存储中：" + relativePath);
        }

        Stream content = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult(new StoredFileContent(
            content,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            string.IsNullOrWhiteSpace(fileName) ? Path.GetFileName(absolutePath) : fileName,
            content.Length));
    }
}
