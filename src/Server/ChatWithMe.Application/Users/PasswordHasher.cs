using System.Globalization;
using System.Security.Cryptography;

namespace ChatWithMe.Application.Users;

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const string Algorithm = "pbkdf2-sha256";

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new ArgumentException("密码长度不能少于 6 个字符。", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return Algorithm + "$" + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(key);
    }

    public static bool Verify(string? password, string? hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var parts = hash.Split('$');

        if (parts.Length != 4 || !string.Equals(parts[0], Algorithm, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);

            if (salt.Length == 0 || expected.Length != KeySize)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
