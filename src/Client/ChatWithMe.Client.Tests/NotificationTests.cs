using System.Net;
using System.Net.Http.Json;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Services;
using ChatWithMe.Client.ViewModels;

namespace ChatWithMe.Client.Tests;

public sealed class NotificationTests
{
    [Fact]
    public async Task OpenLatestDetail_IgnoresSlowerPreviousResponse()
    {
        var first = new AnnouncementDto(Guid.NewGuid(), "第一条", "旧目标", "2026/11/19 12:00:00");
        var latest = new AnnouncementDto(Guid.NewGuid(), "第二条", "最新目标", "2026/11/19 12:01:00");
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var api = new ApiClient(new HttpClient(new AsyncHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith(first.Id.ToString(), StringComparison.Ordinal))
            {
                await release.Task;
                return first;
            }
            return latest;
        })) { BaseAddress = new Uri("http://localhost/") });
        await using var connection = new ChatConnection(api, new Session());
        using var vm = new NotificationsPageViewModel(api, connection);
        var pending = vm.OpenAsync(first.Id);
        await vm.OpenAsync(latest.Id);
        release.SetResult();
        await pending;
        Assert.Equal(latest, vm.SelectedNotification);
    }

    [Fact]
    public async Task Refresh_PlacesNewOfflineNotificationsBeforeHistory()
    {
        var older = new AnnouncementDto(Guid.NewGuid(), "旧通知", "旧", "2026/11/18 12:00:00");
        var newer = new AnnouncementDto(Guid.NewGuid(), "新通知", "新", "2026/11/19 12:00:00");
        var first = true;
        using var api = new ApiClient(new HttpClient(new Handler(_ => new AnnouncementPage(first ? [older] : [newer, older], 2, 1, 20)))
            { BaseAddress = new Uri("http://localhost/") });
        await using var connection = new ChatConnection(api, new Session());
        using var vm = new NotificationsPageViewModel(api, connection);
        await vm.RefreshAsync();
        first = false;
        await vm.RefreshAsync();
        Assert.Equal(new[] { newer.Id, older.Id }, vm.Notifications.Select(x => x.Id));
    }

    [Fact]
    public async Task RefreshDuringPendingLoad_IsRetriedAfterLoadCompletes()
    {
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var newest = new AnnouncementDto(Guid.NewGuid(), "断线期间通知", "内容", "2026/11/19 12:00:00");
        using var api = new ApiClient(new HttpClient(new AsyncHandler(async _ =>
        {
            if (++calls == 1)
            {
                started.SetResult();
                await waiting.Task;
                return new AnnouncementPage([], 0, 1, 20);
            }
            return new AnnouncementPage([newest], 1, 1, 20);
        })) { BaseAddress = new Uri("http://localhost/") });
        await using var connection = new ChatConnection(api, new Session());
        using var vm = new NotificationsPageViewModel(api, connection);
        var loading = vm.RefreshAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await vm.RefreshAsync();
        waiting.SetResult();
        await loading.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(newest, Assert.Single(vm.Notifications));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Refresh_DeduplicatesHistory_AndDetailLoadsFullBody()
    {
        var item = new AnnouncementDto(Guid.NewGuid(), "标题", "正文\n详情", "2026/11/19 12:00:00");
        using var api = new ApiClient(new HttpClient(new Handler(request =>
            request.RequestUri!.AbsolutePath.EndsWith(item.Id.ToString(), StringComparison.Ordinal)
                ? item : new AnnouncementPage([item], 1, 1, 20))) { BaseAddress = new Uri("http://localhost/") });
        var session = new Session();
        await using var connection = new ChatConnection(api, session);
        using var vm = new NotificationsPageViewModel(api, connection);

        await vm.RefreshAsync();
        await vm.RefreshAsync();
        await vm.OpenAsync(item.Id);

        Assert.Equal(item, Assert.Single(vm.Notifications));
        Assert.False(vm.HasMore);
        Assert.Equal(item, vm.SelectedNotification);
    }

    [Fact]
    public async Task LoadFailure_RaisesErrorAndAllowsRetry()
    {
        var fail = true;
        using var api = new ApiClient(new HttpClient(new Handler(_ => fail
            ? throw new HttpRequestException("断网") : new AnnouncementPage([], 0, 1, 20))) { BaseAddress = new Uri("http://localhost/") });
        await using var connection = new ChatConnection(api, new Session());
        using var vm = new NotificationsPageViewModel(api, connection);
        var errors = new List<string>();
        vm.ErrorOccurred += errors.Add;
        await vm.RefreshAsync();
        Assert.Contains("断网", Assert.Single(errors));
        Assert.False(vm.IsBusy);
        fail = false;
        await vm.RefreshAsync();
        Assert.Single(errors);
    }

    private sealed class Handler(Func<HttpRequestMessage, object> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(respond(request)) });
    }

    private sealed class AsyncHandler(Func<HttpRequestMessage, Task<object>> respond) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            new(HttpStatusCode.OK) { Content = JsonContent.Create(await respond(request)) };
    }
}
