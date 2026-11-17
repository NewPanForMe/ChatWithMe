using Avalonia.Controls;
using TKCode.Arc;
using ChatWithMe.Client.Settings;

namespace ChatWithMe.Client.Views;

/// <summary>The desktop window around <see cref="MainView"/>.</summary>
public partial class MainWindow : ArcWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // Opens where it was last closed, at the same size, and remembers the placement when it closes.
        AppSettings.Current.TrackWindow(this);
    }
}
