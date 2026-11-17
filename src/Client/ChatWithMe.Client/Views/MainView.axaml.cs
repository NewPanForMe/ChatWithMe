using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatWithMe.Client.Settings;
using TKCode.Arc;

namespace ChatWithMe.Client.Views;

/// <summary>The app's content: the main window shows it on the desktop, and the page itself in a browser.</summary>
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        // The sidebar opens as it was left: folded to the icon rail or not.
        Nav.IsCollapsed = AppSettings.Current.IsSidebarCollapsed;
        // The page shown follows MainViewModel.SelectedKey, which the sidebar's SelectedKey is bound to.
        Nav.PropertyChanged += OnNavPropertyChanged;
    }

    private void OnNavPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Sidebar.IsCollapsedProperty && AppSettings.Current.IsSidebarCollapsed != Nav.IsCollapsed)
        {
            AppSettings.Current.IsSidebarCollapsed = Nav.IsCollapsed;
            AppSettings.Current.Save();
        }
    }

    private void OnToggleSidebar(object? sender, RoutedEventArgs e) => Nav.ToggleCollapsed();
}
