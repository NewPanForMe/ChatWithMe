namespace ChatWithMe.Application.Common;

/// <summary>
/// 统一分页结果。Total 为满足过滤条件的总条数（不受分页影响）。
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
