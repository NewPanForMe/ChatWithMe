using System.Security.Cryptography;
using System.Text;
using ChatWithMe.Application.Auth;

namespace ChatWithMe.Infrastructure.Auth;

public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("令牌不能为空。", nameof(token));
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }
}
