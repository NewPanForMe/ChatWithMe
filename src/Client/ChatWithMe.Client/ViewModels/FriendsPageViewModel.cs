using System.Collections.ObjectModel;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatWithMe.Client.ViewModels;

public sealed partial class FriendsPageViewModel(ApiClient apiClient, Session session) : ObservableObject
{
    private bool _initialized;

    public ObservableCollection<UserSummaryDto> SearchResults { get; } = [];

    public ObservableCollection<FriendRequestDto> IncomingRequests { get; } = [];

    public ObservableCollection<FriendDto> Friends { get; } = [];

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    public event Action<string, bool>? NotificationRequested;

    [ObservableProperty]
    private string? _searchStatus;

    public bool HasSearchStatus => !string.IsNullOrWhiteSpace(SearchStatus);

    partial void OnSearchStatusChanged(string? value) => OnPropertyChanged(nameof(HasSearchStatus));

    [ObservableProperty]
    private bool _isBusy;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await RefreshListsAsync(notifySuccess: false);
    }

    [RelayCommand]
    public Task RefreshAsync() => RefreshListsAsync(notifySuccess: true);

    private async Task RefreshListsAsync(bool notifySuccess)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await LoadListsAsync();
            if (notifySuccess)
            {
                NotificationRequested?.Invoke("刷新成功。", true);
            }
        }
        catch (Exception exception)
        {
            NotificationRequested?.Invoke("刷新失败：" + exception.Message, false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (IsBusy)
        {
            return;
        }

        var keyword = SearchKeyword.Trim();

        if (keyword.Length == 0)
        {
            NotificationRequested?.Invoke("请输入邮箱或昵称。", false);
            return;
        }

        IsBusy = true;
        SearchStatus = null;

        try
        {
            Replace(SearchResults, await apiClient.SearchUsersForFriendAsync(keyword));

            SearchStatus = SearchResults.Count == 0
                ? "没有找到匹配的用户（不能搜索自己，且只搜索已注册的聊天用户）。"
                : $"找到 {SearchResults.Count} 个用户。";
            NotificationRequested?.Invoke("搜索完成。" + SearchStatus, true);
        }
        catch (Exception exception)
        {
            NotificationRequested?.Invoke("搜索失败：" + exception.Message, false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task AddFriendAsync(UserSummaryDto user)
    {
        if (user.Id == session.UserId)
        {
            NotificationRequested?.Invoke("不能添加自己为好友。", false);
            return;
        }

        await RunAsync(async () =>
            await apiClient.SendFriendRequestAsync(user.Id, "你好，我想加你为好友"), "好友申请已发送。");
    }

    public Task AcceptAsync(FriendRequestDto request) => RunAsync(async () =>
        await apiClient.AcceptFriendRequestAsync(request.Id), "已同意好友申请。");

    public Task RejectAsync(FriendRequestDto request) => RunAsync(async () =>
        await apiClient.RejectFriendRequestAsync(request.Id), "已拒绝好友申请。");

    public Task RemoveAsync(FriendDto friend) => RunAsync(async () =>
    {
        await apiClient.RemoveFriendAsync(friend.UserId);
        Friends.Remove(friend);
    }, "已删除好友。");

    public Task BlockAsync(FriendDto friend) => RunAsync(
        () => apiClient.BlockUserAsync(friend.UserId), "已拉黑该好友（同时解除好友关系）。");

    private async Task LoadListsAsync()
    {
        Replace(Friends, await apiClient.GetFriendsAsync());
        Replace(IncomingRequests, await apiClient.GetIncomingFriendRequestsAsync());
    }

    private async Task RunAsync(Func<Task> action, string successMessage)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;

        try
        {
            await action();
            NotificationRequested?.Invoke(successMessage, true);

            try
            {
                await LoadListsAsync();
            }
            catch (Exception exception)
            {
                NotificationRequested?.Invoke(successMessage + "但列表刷新失败：" + exception.Message, false);
            }
        }
        catch (Exception exception)
        {
            NotificationRequested?.Invoke("操作失败：" + exception.Message, false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
