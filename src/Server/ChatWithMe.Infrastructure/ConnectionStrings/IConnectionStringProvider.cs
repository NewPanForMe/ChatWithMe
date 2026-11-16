namespace ChatWithMe.Infrastructure.ConnectionStrings;

public interface IConnectionStringProvider
{
    string GetConnectionString(string? connName = null);

    Task<string> GetConnectionStringAsync(string? connName = null, CancellationToken cancellationToken = default);
}
