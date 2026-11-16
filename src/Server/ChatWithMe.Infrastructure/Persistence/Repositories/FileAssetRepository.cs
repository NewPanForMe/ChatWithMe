using ChatWithMe.Domain.Files;
using ChatWithMe.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace ChatWithMe.Infrastructure.Persistence.Repositories;

public sealed class FileAssetRepository : IFileAssetRepository
{
    private readonly ChatWithMeDbContext _context;

    public FileAssetRepository(ChatWithMeDbContext context) => _context = context;

    public Task AddAsync(FileAsset fileAsset, CancellationToken cancellationToken)
    {
        _context.FileAssets.Add(fileAsset);
        return Task.CompletedTask;
    }

    public Task<FileAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.FileAssets.AsNoTracking().FirstOrDefaultAsync(fileAsset => fileAsset.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FileAsset>> ListAsync(int skip, int take, string? keyword, CancellationToken cancellationToken) =>
        await BuildQuery(keyword)
            .OrderByDescending(fileAsset => fileAsset.CreatedAtUtc)
            .ThenByDescending(fileAsset => fileAsset.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<int> CountAsync(string? keyword, CancellationToken cancellationToken) =>
        BuildQuery(keyword).CountAsync(cancellationToken);

    private IQueryable<FileAsset> BuildQuery(string? keyword)
    {
        var query = _context.FileAssets.AsNoTracking();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var pattern = LikePatterns.Contains(keyword);

        return query.Where(fileAsset => EF.Functions.ILike(fileAsset.OriginalName, pattern, LikePatterns.EscapeCharacter));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
