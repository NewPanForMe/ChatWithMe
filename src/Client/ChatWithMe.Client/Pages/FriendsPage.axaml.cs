using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Navigation;
using ChatWithMe.Client.Services;
using ChatWithMe.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using TKCode.Arc;

namespace ChatWithMe.Client.Pages;

public partial class FriendsPage : UserControl
{
    private bool _isRemovingFriend;
    private FriendsPageViewModel? _notificationSource;

    public FriendsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is FriendsPageViewModel viewModel)
        {
            if (_notificationSource is not null)
            {
                _notificationSource.NotificationRequested -= OnNotificationRequested;
            }

            _notificationSource = viewModel;
            viewModel.NotificationRequested += OnNotificationRequested;
            _ = viewModel.InitializeAsync();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        if (_notificationSource is not null)
        {
            _notificationSource.NotificationRequested -= OnNotificationRequested;
            _notificationSource = null;
        }

        base.OnUnloaded(e);
    }

    private void OnNotificationRequested(string message, bool success) =>
        Toast.Show(this, success ? "操作成功" : "操作失败",
            success ? ToastKind.Success : ToastKind.Error, message);

    private void OnAddFriendClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: UserSummaryDto user } && DataContext is FriendsPageViewModel viewModel)
        {
            _ = viewModel.AddFriendAsync(user);
        }
    }

    private void OnAcceptClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: FriendRequestDto request } && DataContext is FriendsPageViewModel viewModel)
        {
            _ = viewModel.AcceptAsync(request);
        }
    }

    private void OnRejectClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: FriendRequestDto request } && DataContext is FriendsPageViewModel viewModel)
        {
            _ = viewModel.RejectAsync(request);
        }
    }

    private async void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (_isRemovingFriend || sender is not Control { DataContext: FriendDto friend }
            || DataContext is not FriendsPageViewModel viewModel || viewModel.IsBusy)
        {
            return;
        }

        _isRemovingFriend = true;

        try
        {
            var confirmed = await Dialog.ConfirmAsync(
                this, "删除好友", $"确定删除好友“{friend.NickName}”（{friend.Email}）吗？",
                "确认删除", "取消", true);

            if (confirmed)
            {
                await viewModel.RemoveAsync(friend);
            }
        }
        finally
        {
            _isRemovingFriend = false;
        }
    }

    private void OnChatClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: FriendDto friend })
        {
            return;
        }

        var chat = App.Services.GetRequiredService<ChatPageViewModel>();

        var main = App.Services.GetRequiredService<MainViewModel>();
        main.SelectedKey = AppPages.Chat;

        _ = chat.OpenChatWithFriendAsync(friend);
    }
}
