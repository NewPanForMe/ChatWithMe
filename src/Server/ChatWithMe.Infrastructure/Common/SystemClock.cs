using ChatWithMe.Application.Abstractions;

namespace ChatWithMe.Infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
