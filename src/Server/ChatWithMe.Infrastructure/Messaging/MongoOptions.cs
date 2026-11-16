namespace ChatWithMe.Infrastructure.Messaging;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public const string MessagesCollectionName = "messages";

    /// <summary>
    /// 远端实例上已存在同名小写库 chatwithme（MongoDB 拒绝创建仅大小写不同的库），
    /// 因此默认值保持小写，与远端现状一致。
    /// </summary>
    public string DatabaseName { get; set; } = "chatwithme";

    public int ServerSelectionTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// 认证库（MongoDB 的 authSource）。远端下发的连接串未携带 authSource 时用此值补齐；
    /// 连接串自身已带 authSource 则以连接串为准。留空表示不干预，沿用驱动默认（即连接串中的数据库名）。
    /// </summary>
    public string AuthSource { get; set; } = string.Empty;
}
