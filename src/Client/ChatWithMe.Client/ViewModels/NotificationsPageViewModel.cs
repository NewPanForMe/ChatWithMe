using System.Collections.ObjectModel;
using Avalonia.Threading;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatWithMe.Client.ViewModels;

public sealed partial class NotificationsPageViewModel : ObservableObject, IDisposable
{
    private readonly ApiClient _api;
    private readonly ChatConnection _connection;
    private int _nextPage = 1;
    private bool _disposed;
    private bool _refreshPending;
    private readonly HashSet<Guid> _announced = [];
    private int _detailVersion;

    public NotificationsPageViewModel(ApiClient api, ChatConnection connection)
    {
        _api = api;
        _connection = connection;
        connection.NotificationReceived += OnReceived;
        connection.Reconnected += OnReconnected;
    }

    public ObservableCollection<AnnouncementDto> Notifications { get; } = [];
    public event Action<AnnouncementDto>? NotificationArrived;
    public event Action<string>? ErrorOccurred;
    [ObservableProperty] private AnnouncementDto? _selectedNotification;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _hasMore;

    public async Task InitializeAsync()
    {
        try { await _connection.EnsureStartedAsync(); }
        catch (Exception exception) { ErrorOccurred?.Invoke("通知实时连接失败：" + exception.Message); }
        await RefreshAsync();
    }

    private void OnReceived(AnnouncementDto item) => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed || !_announced.Add(item.Id)) return;
        if (!Notifications.Any(x => x.Id == item.Id)) InsertSorted(item);
        NotificationArrived?.Invoke(item);
    });

    private void OnReconnected() => Dispatcher.UIThread.Post(() => { if (!_disposed) _ = RefreshAsync(); });

    [RelayCommand]
    public Task RefreshAsync() => LoadAsync(1);

    [RelayCommand]
    private Task LoadMoreAsync() => HasMore ? LoadAsync(_nextPage) : Task.CompletedTask;

    private async Task LoadAsync(int page)
    {
        if (_disposed) return;
        if (IsBusy)
        {
            if (page == 1) _refreshPending = true;
            return;
        }
        IsBusy = true;
        try
        {
            var result = await _api.GetNotificationsAsync(page);
            if (_disposed) return;
            foreach (var item in result.Items)
            {
                if (!Notifications.Any(x => x.Id == item.Id)) InsertSorted(item);
            }
            _nextPage = page + 1;
            HasMore = result.Page * result.PageSize < result.Total;
        }
        catch (Exception exception) { if (!_disposed) ErrorOccurred?.Invoke("加载通知失败：" + exception.Message); }
        finally { IsBusy = false; }
        if (_refreshPending && !_disposed)
        {
            _refreshPending = false;
            await LoadAsync(1);
        }
    }

    private void InsertSorted(AnnouncementDto item)
    {
        var index = 0;
        while (index < Notifications.Count)
        {
            var comparison = string.CompareOrdinal(Notifications[index].CreatedAtUtc, item.CreatedAtUtc);
            if (comparison < 0 || (comparison == 0 && Notifications[index].Id.CompareTo(item.Id) < 0)) break;
            index++;
        }
        Notifications.Insert(index, item);
    }

    public async Task OpenAsync(Guid id)
    {
        if (_disposed) return;
        var version = ++_detailVersion;
        SelectedNotification = Notifications.FirstOrDefault(x => x.Id == id);
        try
        {
            var detail = await _api.GetNotificationAsync(id);
            if (!_disposed && version == _detailVersion) SelectedNotification = detail;
        }
        catch (Exception exception)
        {
            if (!_disposed && version == _detailVersion) ErrorOccurred?.Invoke("读取通知详情失败：" + exception.Message);
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _connection.NotificationReceived -= OnReceived;
        _connection.Reconnected -= OnReconnected;
        Notifications.Clear();
        _announced.Clear();
        SelectedNotification = null;
    }
}
