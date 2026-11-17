using ChatWithMe.Api.Auth;
using ChatWithMe.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminUsersController(IAdminUserAppService adminUserAppService) : ControllerBase
{
    [HttpGet("online")]
    public async Task<ActionResult<IReadOnlyList<OnlineUserDto>>> ListOnlineAsync(CancellationToken cancellationToken) =>
        Ok(await adminUserAppService.ListOnlineAsync(cancellationToken));

    [HttpPost("{id:guid}/kick")]
    public async Task<ActionResult<KickResultDto>> KickAsync(Guid id, CancellationToken cancellationToken) =>
        Ok(await adminUserAppService.KickAsync(id, cancellationToken));
}
