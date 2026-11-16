namespace ChatWithMe.Domain.Users;

public sealed class Email : IEquatable<Email>
{
    private Email() => Value = string.Empty;

    private Email(string value) => Value = value;

    public string Value { get; private set; }

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("邮箱不能为空。", nameof(value));
        }

        var normalized = value.Trim().ToLowerInvariant();
        var atIndex = normalized.IndexOf('@');

        if (normalized.Length > 256
            || atIndex <= 0
            || atIndex == normalized.Length - 1
            || normalized.IndexOf('@', atIndex + 1) >= 0
            || normalized.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException($"邮箱格式不正确：{value}", nameof(value));
        }

        return new Email(normalized);
    }

    public bool Equals(Email? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Email);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
