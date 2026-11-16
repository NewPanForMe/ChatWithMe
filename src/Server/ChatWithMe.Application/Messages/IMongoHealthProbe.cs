namespace ChatWithMe.Application.Messages;

public interface IMongoHealthProbe
{
    Task<MongoHealthResult> CheckAsync(CancellationToken cancellationToken);
}

public sealed record MongoHealthResult(bool Ok, string Detail);
