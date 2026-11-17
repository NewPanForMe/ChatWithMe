using System.Net;
using System.Text.RegularExpressions;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatWithMe.Client.ViewModels;

public sealed partial class LoginViewModel(ApiClient apiClient, Session session) : ObservableObject
{
    private static readonly Regex EmailPattern = new("^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$", RegexOptions.Compiled);

    public event EventHandler? LoggedIn;

    public string BaseAddress => apiClient.BaseAddress.ToString();

    // 仅用于本地联调：设置环境变量后预填登录框，不会自动登录。
    [ObservableProperty]
    private string _email = Environment.GetEnvironmentVariable("CHATWITHME_TEST_EMAIL") ?? string.Empty;

    [ObservableProperty]
    private string _password = Environment.GetEnvironmentVariable("CHATWITHME_TEST_PASSWORD") ?? string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoginMode))]
    [NotifyPropertyChangedFor(nameof(IsRegisterMode))]
    private bool _isRegistering;

    public bool IsLoginMode => !IsRegistering;

    public bool IsRegisterMode => IsRegistering;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    partial void OnIsRegisteringChanged(bool value)
    {
        ErrorMessage = null;
        ConfirmPassword = string.Empty;
    }

    [RelayCommand]
    private void ShowRegister()
    {
        ErrorMessage = null;
        IsRegistering = true;
    }

    [RelayCommand]
    private void ShowLogin()
    {
        ErrorMessage = null;
        IsRegistering = false;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "请输入邮箱和密码。";
            return;
        }

        IsBusy = true;

        try
        {
            await SignInAsync(Email.Trim(), Password);
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = null;
        var email = Email.Trim();

        if (!IsValidEmail(email))
        {
            ErrorMessage = "邮箱格式不正确，请检查后重试。";
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "密码至少 6 位。";
            return;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "两次输入的密码不一致。";
            return;
        }

        IsBusy = true;

        try
        {
            var nickName = email[..email.IndexOf('@')];

            if (nickName.Length == 0)
            {
                nickName = "用户";
            }

            if (nickName.Length > 50)
            {
                nickName = nickName[..50];
            }

            await apiClient.RegisterAsync(email, nickName, Password);
            await SignInAsync(email, Password);
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
        {
            ErrorMessage = "该邮箱已注册，请直接登录。";
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SignInAsync(string email, string password)
    {
        AuthSessionDto sessionDto = await apiClient.LoginAsync(email, password);

        session.SetUser(sessionDto.User);
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        LoggedIn?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsValidEmail(string email) =>
        email.Length <= 256 && EmailPattern.IsMatch(email);
}
