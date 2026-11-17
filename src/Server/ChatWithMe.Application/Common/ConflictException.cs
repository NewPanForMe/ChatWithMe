namespace ChatWithMe.Application.Common;

/// <summary>与当前资源状态冲突（HTTP 409），例如重复申请、已是好友。</summary>
public sealed class ConflictException(string message) : Exception(message);
