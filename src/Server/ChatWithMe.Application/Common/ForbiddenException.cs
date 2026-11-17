namespace ChatWithMe.Application.Common;

/// <summary>已认证但无权访问该资源（HTTP 403）。</summary>
public sealed class ForbiddenException(string message) : Exception(message);
