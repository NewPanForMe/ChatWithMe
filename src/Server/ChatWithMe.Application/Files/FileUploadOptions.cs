namespace ChatWithMe.Application.Files;

public sealed class FileUploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [];

    public string StorageRoot { get; set; } = "wwwroot/uploads";

    public int MaxFilesPerRequest { get; set; } = 5;

    /// <summary>
    /// 回读链接（短时签名 URL）的有效期，单位秒。文件不再通过静态目录匿名暴露，
    /// 直链只在有效期内可访问，过期需重新申请。
    /// </summary>
    public int LinkLifetimeSeconds { get; set; } = 300;
}
