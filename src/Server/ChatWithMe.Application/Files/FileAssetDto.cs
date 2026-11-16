namespace ChatWithMe.Application.Files;

public sealed record FileAssetDto(
    Guid Id,
    string OriginalName,
    string StoredName,
    string RelativePath,
    string ContentType,
    long SizeInBytes,
    string Url,
    DateTime CreatedAtUtc);
