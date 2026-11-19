using Avalonia.Controls;
using TKCode.Arc;
using ChatWithMe.Client.Settings;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.ViewModels;
using ChatWithMe.Client.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace ChatWithMe.Client.Views;

/// <summary>The desktop window around <see cref="MainView"/>.</summary>
public partial class MainWindow : ArcWindow
{
    private NotificationsPageViewModel? _notifications;
    public MainWindow()
    {
        InitializeComponent();
        // Opens where it was last closed, at the same size, and remembers the placement when it closes.
        AppSettings.Current.TrackWindow(this);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _notifications = App.Services.GetRequiredService<NotificationsPageViewModel>();
        _notifications.NotificationArrived += OnNotification;
        _notifications.ErrorOccurred += OnNotificationError;
        _ = _notifications.InitializeAsync();
    }

    private void OnNotification(AnnouncementDto item) => Toast.Show(this, "通知", ToastKind.Info, item.Title,
        new ToastAction("查看详情", () =>
        {
            if (_notifications is null) return;
            if (DataContext is MainViewModel main) main.SelectedKey = AppPages.Notifications;
            _ = _notifications.OpenAsync(item.Id);
        }));

    private void OnNotificationError(string error) => Toast.Show(this, "通知", ToastKind.Error, error);

    protected override void OnClosed(EventArgs e)
    {
        if (_notifications is not null)
        {
            _notifications.NotificationArrived -= OnNotification;
            _notifications.ErrorOccurred -= OnNotificationError;
            _notifications = null;
        }
        base.OnClosed(e);
    }
}
