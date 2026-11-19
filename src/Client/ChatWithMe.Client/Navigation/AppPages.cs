using Avalonia.Controls;
using ChatWithMe.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using ChatWithMe.Client.ViewModels;

namespace ChatWithMe.Client.Navigation;

/// <summary>One page of the app: a stable key (what the navigation selects by), the title the top bar shows and how to create the view.</summary>
public sealed record AppPage(string Key, string Title, Func<Control> Create);

/// <summary>
/// The pages of the app. The navigation (the sidebar in Views/MainView.axaml) names them by <see cref="AppPage.Key"/>;
/// to add a page, create its view in Pages/ (dotnet new tkcode-arc-page), add it here and add a row with the same key to
/// the navigation.
/// Each entry also pairs the view with its view model: written out here, not looked up by type name, so the app has no
/// reflection and can be trimmed.
/// </summary>
public static class AppPages
{
    public const string Chat = "chat";
    public const string Friends = "friends";
    public const string Settings = "settings";
    public const string Notifications = "notifications";

    public static IReadOnlyList<AppPage> All { get; } =
    [
        new(Chat, "聊天", () => new ChatPage { DataContext = App.Services.GetRequiredService<ChatPageViewModel>() }),
        new(Friends, "好友", () => new FriendsPage { DataContext = App.Services.GetRequiredService<FriendsPageViewModel>() }),
        new(Settings, "设置", () => new SettingsPage { DataContext = App.Services.GetRequiredService<SettingsPageViewModel>() }),
        new(Notifications, "通知", () => new NotificationsPage { DataContext = App.Services.GetRequiredService<NotificationsPageViewModel>() }),
    ];

    public static AppPage? Find(string? key)
    {
        foreach (var page in All)
        {
            if (page.Key == key)
            {
                return page;
            }
        }
        return null;
    }
}
