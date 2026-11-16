using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Messages;
using ChatWithMe.Domain.Auth;
using ChatWithMe.Domain.Files;
using ChatWithMe.Domain.Messages;
using ChatWithMe.Domain.Users;
using ChatWithMe.Infrastructure.Auth;
using ChatWithMe.Infrastructure.Common;
using ChatWithMe.Infrastructure.ConnectionStrings;
using ChatWithMe.Infrastructure.Messaging;
using ChatWithMe.Infrastructure.Persistence;
using ChatWithMe.Infrastructure.Persistence.Repositories;
using ChatWithMe.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace ChatWithMe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var timeoutSeconds = configuration.GetValue("Security:ConnectionStringTimeoutSeconds", 20);

        services.AddHttpClient(RuntimeConnectionStringProvider.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 20);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        services.AddSingleton<IConnectionStringProvider, RuntimeConnectionStringProvider>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddDbContext<ChatWithMeDbContext>((serviceProvider, options) =>
            options.UseNpgsql(serviceProvider.GetRequiredService<IConnectionStringProvider>().GetConnectionString()));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFileAssetRepository, FileAssetRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IMessageRepository, MongoMessageRepository>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();

        services.AddSingleton(typeof(Lazy<>), typeof(LazyService<>));
        services.AddSingleton<IMongoClient>(MongoConnectionSettings.CreateClient);
        services.AddSingleton<IMongoHealthProbe, MongoHealthProbe>();
        services.AddHostedService<MongoIndexInitializer>();

        return services;
    }
}
