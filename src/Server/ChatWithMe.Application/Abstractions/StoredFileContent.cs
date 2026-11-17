namespace ChatWithMe.Application.Abstractions;

/// <summary>存储层读回的文件内容：调用方负责释放 <see cref="Content"/>。</summary>
public sealed record StoredFileContent(Stream Content, string ContentType, string FileName, long LengthInBytes);
