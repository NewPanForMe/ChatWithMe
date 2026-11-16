using ChatWithMe.Application.Common;
using ChatWithMe.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserAppService userAppService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        var result = await userAppService.ListAsync(page, pageSize, keyword, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> CreateAsync([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await userAppService.CreateAsync(request, cancellationToken);

        return Created("/api/users/" + user.Id, user);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userAppService.GetAsync(id, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }
}
