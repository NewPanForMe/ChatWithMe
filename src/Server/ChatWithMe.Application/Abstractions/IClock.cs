namespace ChatWithMe.Application.Abstractions;

/// <summary>
/// 可替换的时钟抽象，业务层取"当前 UTC 时间"统一走这里，便于测试与统一口径。
/// 实体内部的时间戳仍由领域层自行写入，二者都只产出 UTC。
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
