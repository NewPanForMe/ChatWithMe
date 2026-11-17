using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using ChatWithMe.Client.Services;
using ChatWithMe.Client.Settings;
using ChatWithMe.Client.ViewModels;
using ChatWithMe.Client.Views;

namespace ChatWithMe.Client;

/// <summary>
/// The application. Arc needs nothing here: no base class, no App.axaml and no styles. UseArcApp() (AppSetup.cs)
/// registers the themes, and Arc's base theme styles the window and the Avalonia controls Arc's templates use.
/// </summary>
public class App : Application
{
    private IServiceProvider _rootServices = null!;
    private AsyncServiceScope _sessionScope;

    /// <summary>The app's services (Services/AppServices.cs), built when the app starts.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void OnFrameworkInitializationCompleted()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => WriteCrash(args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) => { WriteCrash(args.Exception); args.SetObserved(); };
        Dispatcher.UIThread.UnhandledException += (_, args) => WriteCrash(args.Exception);

        _rootServices = AppServices.Build();
        BeginSession();
        // Every theme change (the top bar's switch, the Settings page, your own ArcTheme.Apply calls) is saved.
        AppSettings.Current.TrackTheme();
        switch (ApplicationLifetime)
        {
            // A desktop window: start with the login window, it opens the main window once signed in.
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new LoginWindow(Services.GetRequiredService<LoginViewModel>());
                break;
            // A browser page or a phone screen: the content without a window.
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = new MainView { DataContext = CreateMainViewModel() };
                break;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static MainViewModel CreateMainViewModel() => Services.GetRequiredService<MainViewModel>();

    private void BeginSession()
    {
        _sessionScope = _rootServices.CreateAsyncScope();
        Services = _sessionScope.ServiceProvider;
        var sessionServices = Services;
        Services.GetRequiredService<ApiClient>().SessionExpired += () =>
            Dispatcher.UIThread.Post(() => _ = ReturnToLoginAsync(sessionServices));
    }

    private async Task ReturnToLoginAsync(IServiceProvider expiredServices)
    {
        // 多个并发请求只处理当前会话一次，旧会话的迟到通知不会影响重新登录。
        if (!ReferenceEquals(Services, expiredServices)
            || ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop
            || desktop.MainWindow is not MainWindow main)
        {
            return;
        }

        expiredServices.GetRequiredService<ApiClient>().ClearToken();
        expiredServices.GetRequiredService<Session>().Clear();
        var expiredScope = _sessionScope;
        BeginSession();

        var loginViewModel = Services.GetRequiredService<LoginViewModel>();
        loginViewModel.Password = string.Empty;
        loginViewModel.ErrorMessage = "账号已被强制下线或会话已失效，请重新登录。";
        var login = new LoginWindow(loginViewModel, allowAutoLogin: false);
        desktop.MainWindow = login;
        login.Show();
        main.Close();

        try
        {
            await expiredScope.DisposeAsync();
        }
        catch (Exception exception)
        {
            WriteCrash(exception);
        }
    }

    private static void WriteCrash(object? exception)
    {
        try
        {
            File.AppendAllText(
                Path.Combine(AppContext.BaseDirectory, "client-crash.log"),
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + exception + Environment.NewLine);
        }
        catch
        {
            // 崩溃日志只用于排查，失败不处理。
        }
    }
}
