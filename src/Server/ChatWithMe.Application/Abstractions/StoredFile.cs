namespace ChatWithMe.Application.Abstractions;

public sealed record StoredFile(string StoredName, string RelativePath, long SizeInBytes);
