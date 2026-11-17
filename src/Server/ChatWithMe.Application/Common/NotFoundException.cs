namespace ChatWithMe.Application.Common;

/// <summary>资源不存在（HTTP 404）。</summary>
public sealed class NotFoundException(string message) : Exception(message);
