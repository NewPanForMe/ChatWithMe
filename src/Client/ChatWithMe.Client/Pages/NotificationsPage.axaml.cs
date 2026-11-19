using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.ViewModels;

namespace ChatWithMe.Client.Pages;

public partial class NotificationsPage : UserControl
{
    public NotificationsPage() => InitializeComponent();

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: AnnouncementDto item } && DataContext is NotificationsPageViewModel vm)
            _ = vm.OpenAsync(item.Id);
    }
}
