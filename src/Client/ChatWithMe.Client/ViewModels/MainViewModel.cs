using CommunityToolkit.Mvvm.ComponentModel;
using ChatWithMe.Client.Navigation;
using ChatWithMe.Client.Services;

namespace ChatWithMe.Client.ViewModels;

/// <summary>
/// The main view: which page is shown. The navigation's selected key and the page host's Page are bound to it, so
/// navigating from code is setting <see cref="SelectedKey"/>.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly Session _session;

    [ObservableProperty]
    private string? _selectedKey;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    private AppPage? _currentPage;

    public MainViewModel(Session session)
    {
        _session = session;
        SelectedKey = AppPages.Chat;
    }

    /// <summary>The shown page's title, for the top bar.</summary>
    public string? Title => CurrentPage?.Title;

    /// <summary>Signed-in person, shown in the sidebar footer.</summary>
    public string DisplayName => _session.DisplayName;

    partial void OnSelectedKeyChanged(string? value)
    {
        // A key that names no page (a row without one) keeps the current page.
        if (AppPages.Find(value) is { } page)
        {
            CurrentPage = page;
        }
    }
}
