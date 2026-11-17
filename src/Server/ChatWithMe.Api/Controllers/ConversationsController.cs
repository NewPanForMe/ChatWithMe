using ChatWithMe.Api.Extensions;
using ChatWithMe.Application.Conversations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController(IConversationAppService conversationAppService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationDto>> CreateAsync(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "访问令牌缺少可用的 sub 声明。",
            });
        }

        var conversation = await conversationAppService.CreateAsync(userId.Value, request, cancellationToken);

        return Created("/api/conversations/" + conversation.Id, conversation);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> ListAsync(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "访问令牌缺少可用的 sub 声明。",
            });
        }

        var conversations = await conversationAppService.ListAsync(userId.Value, cancellationToken);

        return Ok(conversations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "访问令牌缺少可用的 sub 声明。",
            });
        }

        var conversation = await conversationAppService.GetAsync(userId.Value, id, cancellationToken);

        return Ok(conversation);
    }
}
