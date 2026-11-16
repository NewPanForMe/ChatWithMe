using ChatWithMe.Domain.Common;

namespace ChatWithMe.Domain.Users;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, string NickName, DateTime OccurredOnUtc) : IDomainEvent;
