using ChatWithMe.Application.Messages;
using ChatWithMe.Domain.Messages;

namespace ChatWithMe.Tests;

public sealed class MessageTests
{
    [Fact]
    public void Send_TrimsContent()
    {
        var message = Message.Send(Guid.NewGuid(), Guid.NewGuid(), "  hello  ");

        Assert.Equal("hello", message.Content);
    }

    [Fact]
    public void Send_EmptyContent_Throws()
    {
        Assert.Throws<ArgumentException>(() => Message.Send(Guid.NewGuid(), Guid.NewGuid(), "   "));
    }

    [Fact]
    public void Send_TooLongContent_Throws()
    {
        var content = new string('x', Message.MaxContentLength + 1);

        Assert.Throws<ArgumentException>(() => Message.Send(Guid.NewGuid(), Guid.NewGuid(), content));
    }

    [Fact]
    public void Send_TooLongClientMessageId_Throws()
    {
        var clientMessageId = new string('x', Message.MaxClientMessageIdLength + 1);

        Assert.Throws<ArgumentException>(() => Message.Send(Guid.NewGuid(), Guid.NewGuid(), "hi", clientMessageId));
    }

    [Fact]
    public void Send_BlankClientMessageId_BecomesNull()
    {
        var message = Message.Send(Guid.NewGuid(), Guid.NewGuid(), "hi", "   ");

        Assert.Null(message.ClientMessageId);
    }

    [Fact]
    public void SendMessageRequest_ClientMessageIdIsOptional()
    {
        var request = new SendMessageRequest(Guid.NewGuid(), "hi");

        Assert.Null(request.ClientMessageId);
    }
}
