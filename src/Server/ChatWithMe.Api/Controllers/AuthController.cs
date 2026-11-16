using ChatWithMe.Application.Auth;
using ChatWithMe.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthAppService authAppService) : ControllerBase
{
    private const string UnauthorizedTitle = "身份验证失败";

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenDto>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authAppService.LoginAsync(request, cancellationToken));
        }
        catch (AuthenticationFailedException exception)
        {
            return UnauthorizedProblem(exception.Message);
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenDto>> RefreshAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await authAppService.RefreshAsync(request, cancellationToken));
        }
        catch (AuthenticationFailedException exception)
        {
            return UnauthorizedProblem(exception.Message);
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> LogoutAsync([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authAppService.LogoutAsync(request, cancellationToken);

        return NoContent();
    }

    private ObjectResult UnauthorizedProblem(string detail) => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = UnauthorizedTitle,
        Detail = detail,
    });
}
