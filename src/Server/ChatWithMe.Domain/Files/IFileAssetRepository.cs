namespace ChatWithMe.Domain.Files;

public interface IFileAssetRepository
{
    Task AddAsync(FileAsset fileAsset, CancellationToken cancellationToken);

    Task<FileAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// 按创建时间倒序分页查询文件元数据；keyword 非空时对原始文件名做大小写不敏感的子串匹配。
    /// </summary>
    Task<IReadOnlyList<FileAsset>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken);

    Task<int> CountAsync(string? keyword, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
