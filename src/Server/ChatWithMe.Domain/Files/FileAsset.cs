namespace ChatWithMe.Domain.Files;

public sealed class FileAsset
{
    private FileAsset()
    {
    }

    private FileAsset(Guid id, string originalName, string storedName, string relativePath, string contentType, long sizeInBytes)
    {
        Id = id;
        OriginalName = originalName;
        StoredName = storedName;
        RelativePath = relativePath;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string OriginalName { get; private set; } = string.Empty;

    public string StoredName { get; private set; } = string.Empty;

    public string RelativePath { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeInBytes { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static FileAsset Create(string originalName, string storedName, string relativePath, string contentType, long sizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(storedName))
        {
            throw new ArgumentException("存储文件名不能为空。", nameof(storedName));
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("存储相对路径不能为空。", nameof(relativePath));
        }

        if (sizeInBytes <= 0)
        {
            throw new ArgumentException("文件大小必须大于 0。", nameof(sizeInBytes));
        }

        var safeOriginalName = string.IsNullOrWhiteSpace(originalName) ? storedName : originalName.Trim();

        return new FileAsset(
            Guid.NewGuid(),
            safeOriginalName.Length > 260 ? safeOriginalName[..260] : safeOriginalName,
            storedName,
            relativePath,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            sizeInBytes);
    }
}
