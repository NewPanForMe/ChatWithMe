using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ChatWithMe.Application.Abstractions;
using ChatWithMe.Application.Auth;
using ChatWithMe.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatWithMe.Infrastructure.Auth;

public sealed class JwtTokenIssuer : ITokenIssuer
{
    private const int RefreshTokenSizeBytes = 32;

    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenIssuer(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(_options.GetSigningKeyBytes()),
            SecurityAlgorithms.HmacSha256);
    }

    public IssuedTokenPair Issue(Guid userId, string email, UserRole role, int sessionVersion, string principalType)
    {
        var issuedAt = _clock.UtcNow;
        var accessTokenExpiresAtUtc = issuedAt.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var refreshTokenExpiresAtUtc = issuedAt.AddDays(_options.RefreshTokenLifetimeDays);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(AuthClaimNames.Role, role.ToString()),
            new Claim(AuthClaimNames.SessionVersion, sessionVersion.ToString(CultureInfo.InvariantCulture)),
            new Claim(AuthClaimNames.PrincipalType, principalType),
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = issuedAt,
            NotBefore = issuedAt,
            Expires = accessTokenExpiresAtUtc,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = _signingCredentials,
        };

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(handler.CreateToken(descriptor));
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));

        return new IssuedTokenPair(accessToken, accessTokenExpiresAtUtc, refreshToken, refreshTokenExpiresAtUtc);
    }
}
