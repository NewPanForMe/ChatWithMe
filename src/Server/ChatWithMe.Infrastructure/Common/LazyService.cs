using Microsoft.Extensions.DependencyInjection;

namespace ChatWithMe.Infrastructure.Common;

internal sealed class LazyService<T>(IServiceProvider serviceProvider) : Lazy<T>(() => serviceProvider.GetRequiredService<T>())
    where T : class
{
}
