using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Extensions;
using ChatWithMe.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

/// <summary>管理后台账号（后端用户）管理，仅管理员可访问。</summary>
[ApiController]
[Route("api/admin/accounts")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminAccountsController(IAdminAccountAppService adminAccountAppService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAccountDto>>> ListAsync(CancellationToken cancellationToken) =>
        Ok(await adminAccountAppService.ListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdminAccountDto>> CreateAsync(
        [FromBody] CreateAdminAccountRequest request,
        CancellationToken cancellationToken)
    {
        var created = await adminAccountAppService.CreateAsync(request, cancellationToken);

        return Created("/api/admin/accounts/" + created.Id, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var currentAdminId = User.GetUserId();

        if (currentAdminId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "访问令牌缺少可用的 sub 声明。",
            });
        }

        await adminAccountAppService.DeleteAsync(id, currentAdminId.Value, cancellationToken);

        return NoContent();
    }
}
