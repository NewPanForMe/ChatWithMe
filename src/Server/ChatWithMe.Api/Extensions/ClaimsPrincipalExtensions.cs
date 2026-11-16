using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ChatWithMe.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) ? userId : null;
}
