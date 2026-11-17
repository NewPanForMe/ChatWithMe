using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using ChatWithMe.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TKCode.Arc;

namespace ChatWithMe.Client.Views;

public partial class LoginWindow : ArcWindow
{
    private readonly LoginViewModel _viewModel;

    /// <summary>供 XAML 运行时加载器使用（Avalonia 分析器要求有公共无参构造）。</summary>
    public LoginWindow()
        : this(App.Services.GetRequiredService<LoginViewModel>())
    {
    }

    public LoginWindow(LoginViewModel viewModel) : this(viewModel, allowAutoLogin: true)
    {
    }

    public LoginWindow(LoginViewModel viewModel, bool allowAutoLogin)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.LoggedIn += OnLoggedIn;

        // 仅联调：CHATWITHME_AUTOLOGIN=1 时自动提交已预填的账号密码，省去 UI 点击。
        if (allowAutoLogin && Environment.GetEnvironmentVariable("CHATWITHME_AUTOLOGIN") == "1")
        {
            Dispatcher.UIThread.Post(() => _viewModel.LoginCommand.Execute(null));
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.LoggedIn -= OnLoggedIn;
        base.OnClosed(e);
    }

    private void OnLoggedIn(object? sender, EventArgs e)
    {
        var main = new MainWindow { DataContext = App.Services.GetRequiredService<MainViewModel>() };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = main;
        }

        main.Show();
        Close();
    }
}
