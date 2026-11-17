using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using ChatWithMe.Client.Models;
using ChatWithMe.Client.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChatWithMe.Client.ViewModels;

public sealed partial class ChatPageViewModel(ApiClient apiClient, Session session, ChatConnection chatConnection) : ObservableObject
{
    private readonly Dictionary<Guid, UserDto> _userCache = new();
    private CancellationTokenSource? _timeVisibilitySource;
    private bool _initialized;
    private bool _realtimeSubscribed;

    public ObservableCollection<ConversationItem> Conversations { get; } = [];

    public ObservableCollection<FriendDto> Friends { get; } = [];

    public ObservableCollection<MessageItem> Messages { get; } = [];

    [ObservableProperty]
    private ConversationItem? _selectedConversation;

    [ObservableProperty]
    private string _draft = string.Empty;

    [ObservableProperty]
    private string _conversationTitle = "尚未选择会话";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await RefreshAsync();
        await StartRealtimeAsync();
    }

    private async Task StartRealtimeAsync()
    {
        if (_realtimeSubscribed)
        {
            return;
        }

        _realtimeSubscribed = true;
        chatConnection.MessageReceived += OnMessageReceived;
        chatConnection.Reconnected += OnReconnected;

        try
        {
            await chatConnection.EnsureStartedAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = "实时连接失败（消息仍需手动刷新）：" + exception.Message;
            return;
        }

        await JoinAllConversationsAsync();
    }

    private async Task JoinAllConversationsAsync()
    {
        foreach (var conversation in Conversations)
        {
            await chatConnection.JoinConversationAsync(conversation.Id);
        }
    }

    private void OnMessageReceived(MessageDto message) =>
        Dispatcher.UIThread.Post(() => AppendIncomingMessage(message));

    private void OnReconnected() => Dispatcher.UIThread.Post(() => _ = JoinAllConversationsAsync());

    private void AppendIncomingMessage(MessageDto message)
    {

        if (SelectedConversation is null || message.ConversationId != SelectedConversation.Id)
        {
            return;
        }

        if (Messages.Any(existing => existing.Id == message.Id))
        {
            return;
        }

        Messages.Add(ToItem(NormalizeTime(message)));
    }


    /// <summary>SignalR 推送的时间是 ISO-8601；REST 返回的是 yyyy/MM/dd HH:mm:ss。统一成后者。</summary>
    private static MessageDto NormalizeTime(MessageDto message)
    {
        if (DateTime.TryParseExact(message.SentAtUtc, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return message;
        }

        return DateTime.TryParse(message.SentAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? message with { SentAtUtc = parsed.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) }
            : message;
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            Replace(Friends, await apiClient.GetFriendsAsync());

            var conversations = await apiClient.GetConversationsAsync();
            var items = new List<ConversationItem>(conversations.Count);

            foreach (var conversation in conversations)
            {
                items.Add(await ToItemAsync(conversation));
            }

            var selectedId = SelectedConversation?.Id;

            Conversations.Clear();

            foreach (var item in items)
            {
                Conversations.Add(item);
            }

            SelectedConversation = Conversations.FirstOrDefault(item => item.Id == selectedId) ?? Conversations.FirstOrDefault();

            await JoinAllConversationsAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }


    /// <summary>点击好友：复用或创建单聊会话，然后选中它。</summary>
    public async Task OpenChatWithFriendAsync(FriendDto friend)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var conversation = await apiClient.CreateDirectConversationAsync(friend.UserId);
            IsBusy = false;

            await RefreshAsync();
            SelectedConversation = Conversations.FirstOrDefault(item => item.Id == conversation.Id) ?? SelectedConversation;
            await chatConnection.JoinConversationAsync(conversation.Id);
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (SelectedConversation is null)
        {
            StatusMessage = "请先选择一个会话。";
            return;
        }

        var content = Draft.Trim();

        if (content.Length == 0)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var message = await apiClient.SendMessageAsync(
                SelectedConversation.Id,
                content,
                Guid.NewGuid().ToString("N"));

            Messages.Add(ToItem(message));
            Draft = string.Empty;
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private MessageItem? _hoveredMessage;
    private bool _scrolling;

    /// <summary>滚动消息区时调用：让所有消息显示时间，1.5 秒后自动隐藏。</summary>
    public void ShowTimesTemporarily()
    {
        _timeVisibilitySource?.Cancel();
        _scrolling = true;
        ApplyTimeVisibility();

        _timeVisibilitySource = new CancellationTokenSource();
        _ = EndScrollLaterAsync(_timeVisibilitySource.Token);
    }

    /// <summary>鼠标移动时调用：命中哪条消息就让哪条显示时间。</summary>
    public void SetHoveredMessage(MessageItem? message)
    {
        _hoveredMessage = message;
        ApplyTimeVisibility();
    }

    private async Task EndScrollLaterAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(1500, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _scrolling = false;
        ApplyTimeVisibility();
    }

    private void ApplyTimeVisibility()
    {
        foreach (var message in Messages)
        {
            message.IsTimeVisible = _scrolling || ReferenceEquals(message, _hoveredMessage);
        }
    }

    partial void OnSelectedConversationChanged(ConversationItem? value)
    {
        ConversationTitle = value?.Title ?? "尚未选择会话";

        if (value is not null)
        {
            _ = LoadMessagesAsync(value.Id);
        }
    }

    private async Task LoadMessagesAsync(Guid conversationId)
    {
        try
        {
            var messages = await apiClient.GetMessagesAsync(conversationId);

            Messages.Clear();

            foreach (var message in messages.Reverse())
            {
                Messages.Add(ToItem(message));
            }
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private MessageItem ToItem(MessageDto message)
    {
        var isMine = message.SenderId == session.UserId;

        return new MessageItem(
            message.Id,
            isMine ? "我" : _userCache.TryGetValue(message.SenderId, out var sender) ? sender.NickName : message.SenderId.ToString()[..8],
            message.Content,
            message.SentAtUtc,
            isMine);
    }

    private async Task<ConversationItem> ToItemAsync(ConversationDto conversation)
    {
        string title;

        if (!string.IsNullOrWhiteSpace(conversation.Title))
        {
            title = conversation.Title;
        }
        else
        {
            var peerId = conversation.Members
                .Select(member => member.UserId)
                .FirstOrDefault(userId => userId != session.UserId);

            title = peerId == Guid.Empty ? "单聊" : await GetDisplayNameAsync(peerId);
        }

        var subtitle = conversation.Type == "Group"
            ? $"{conversation.Members.Count} 人"
            : "单聊";

        return new ConversationItem(conversation.Id, title, subtitle, conversation.Type);
    }

    private async Task<string> GetDisplayNameAsync(Guid userId)
    {
        if (_userCache.TryGetValue(userId, out var cached))
        {
            return cached.NickName;
        }

        try
        {
            var user = await apiClient.GetUserAsync(userId);

            if (user is not null)
            {
                _userCache[userId] = user;
                return user.NickName;
            }
        }
        catch (Exception)
        {
            // 名称解析失败不影响列表展示。
        }

        return userId.ToString()[..8];
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
