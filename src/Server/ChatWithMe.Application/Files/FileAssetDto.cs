namespace ChatWithMe.Application.Files;

/// <summary>
/// 文件元数据。<c>Url</c> 指向鉴权回读端点（需要 Bearer），浏览器直开请先调
/// <c>GET /api/files/{id}/link</c> 换取短时签名 URL。
/// </summary>
public sealed record FileAssetDto(
    Guid Id,
    string OriginalName,
    string StoredName,
    string RelativePath,
    string ContentType,
    long SizeInBytes,
    string Url,
    DateTime CreatedAtUtc);

/// <summary>短时签名回读链接：<c>Url</c> 为相对路径，带上 <c>exp</c> 与 <c>sig</c>。</summary>
public sealed record FileLinkDto(string Url, DateTime ExpiresAtUtc);
