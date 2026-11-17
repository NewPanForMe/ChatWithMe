using ChatWithMe.Api.Extensions;
using ChatWithMe.Application.Friendships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public sealed class FriendsController(IFriendAppService friendAppService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> ListAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.ListFriendsAsync(userId, cancellationToken));
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> SearchAsync(
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.SearchUsersAsync(userId, keyword, cancellationToken));
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> ListIncomingAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.ListIncomingRequestsAsync(userId, cancellationToken));
    }

    [HttpGet("requests/outgoing")]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> ListOutgoingAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.ListOutgoingRequestsAsync(userId, cancellationToken));
    }

    [HttpPost("requests")]
    public async Task<ActionResult<FriendRequestDto>> SendRequestAsync(
        [FromBody] SendFriendRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        var created = await friendAppService.SendRequestAsync(userId, request, cancellationToken);

        return Created("/api/friends/requests/" + created.Id, created);
    }

    [HttpPost("requests/{id:guid}/accept")]
    public async Task<ActionResult<FriendRequestDto>> AcceptAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.AcceptAsync(userId, id, cancellationToken));
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<ActionResult<FriendRequestDto>> RejectAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.RejectAsync(userId, id, cancellationToken));
    }

    [HttpPatch("{userId:guid}")]
    public async Task<ActionResult<FriendDto>> SetRemarkAsync(
        Guid userId,
        [FromBody] SetFriendRemarkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.SetRemarkAsync(currentUserId, userId, request.Remark, cancellationToken));
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> RemoveAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        await friendAppService.RemoveFriendAsync(currentUserId, userId, cancellationToken);

        return NoContent();
    }

    [HttpGet("blocked")]
    public async Task<ActionResult<IReadOnlyList<BlockedUserDto>>> ListBlockedAsync(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        return Ok(await friendAppService.ListBlockedAsync(userId, cancellationToken));
    }

    [HttpPost("blocked/{userId:guid}")]
    public async Task<IActionResult> BlockAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        await friendAppService.BlockAsync(currentUserId, userId, cancellationToken);

        return NoContent();
    }

    [HttpDelete("blocked/{userId:guid}")]
    public async Task<IActionResult> UnblockAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId))
        {
            return UnauthorizedProblem();
        }

        await friendAppService.UnblockAsync(currentUserId, userId, cancellationToken);

        return NoContent();
    }

    private bool TryGetUserId(out Guid userId)
    {
        var current = User.GetUserId();
        userId = current ?? Guid.Empty;

        return current is not null;
    }

    private ObjectResult UnauthorizedProblem() => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "身份验证失败",
        Detail = "访问令牌缺少可用的 sub 声明。",
    });
}
