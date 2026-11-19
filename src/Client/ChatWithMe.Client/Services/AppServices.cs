using Microsoft.Extensions.DependencyInjection;
using ChatWithMe.Client.ViewModels;

namespace ChatWithMe.Client.Services;

/// <summary>
/// The app's services. Register a service here and take it as a constructor parameter of a view model; the container
/// creates the view models (App.cs for the main one, Navigation/AppPages.cs for the pages). Each login owns a scope:
/// its API cookies, realtime connection and chat state are discarded when the session expires. Everything is registered
/// with its type written out, so the container needs no reflection over the assembly and the app can be trimmed.
/// </summary>
public static class AppServices
{
    public static IServiceProvider Build()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new ApiClient());
        services.AddScoped<ChatConnection>();
        services.AddScoped<Session>();
        services.AddScoped<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddScoped<ChatPageViewModel>();
        services.AddScoped<NotificationsPageViewModel>();
        services.AddTransient<FriendsPageViewModel>();
        services.AddTransient<SettingsPageViewModel>();
        return services.BuildServiceProvider();
    }
}
