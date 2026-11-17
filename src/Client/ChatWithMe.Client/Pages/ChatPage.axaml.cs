using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ChatWithMe.Client.ViewModels;

namespace ChatWithMe.Client.Pages;

public partial class ChatPage : UserControl
{
    public ChatPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is ChatPageViewModel viewModel)
        {
            viewModel.Messages.CollectionChanged += OnMessagesChanged;
            _ = viewModel.InitializeAsync();
        }
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 新消息（包括 SignalR 推送）到达后滚到底部，否则用户看不到。
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            Dispatcher.UIThread.Post(() =>
            {
                MessageScroll.Offset = new Vector(MessageScroll.Offset.X, MessageScroll.Extent.Height);
            });
        }
    }

    private void OnComposerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not ChatPageViewModel viewModel)
        {
            return;
        }

        if (viewModel.SendCommand.CanExecute(null))
        {
            viewModel.SendCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnFriendClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ChatWithMe.Client.Models.FriendDto friend } && DataContext is ChatPageViewModel viewModel)
        {
            _ = viewModel.OpenChatWithFriendAsync(friend);
        }
    }

    private void OnMessagesWheel(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is ChatPageViewModel viewModel)
        {
            viewModel.ShowTimesTemporarily();
        }
    }

    private void OnMessagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: MessageItem message } && DataContext is ChatPageViewModel viewModel)
        {
            viewModel.SetHoveredMessage(message);
        }
    }

    private void OnMessagesPointerExited(object? sender, PointerEventArgs e)
    {
        if (DataContext is ChatPageViewModel viewModel)
        {
            viewModel.SetHoveredMessage(null);
        }
    }
}
