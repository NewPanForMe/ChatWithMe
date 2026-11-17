namespace ChatWithMe.Application.Abstractions;

/// <summary>
/// 文件回读链接的签名器：文件不再以静态目录匿名暴露，改为"鉴权回读"或
/// "短时签名 URL"两种方式，签名绑定文件 Id 与过期时间。
/// </summary>
public interface IFileLinkSigner
{
    string CreateSignature(Guid fileAssetId, DateTime expiresAtUtc);

    bool Verify(Guid fileAssetId, DateTime expiresAtUtc, string? signature);
}
