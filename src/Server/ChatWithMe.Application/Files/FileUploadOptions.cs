namespace ChatWithMe.Application.Files;

public sealed class FileUploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [];

    public string StorageRoot { get; set; } = "wwwroot/uploads";

    public int MaxFilesPerRequest { get; set; } = 5;

    public string PublicUrlPrefix { get; set; } = "/uploads";
}
