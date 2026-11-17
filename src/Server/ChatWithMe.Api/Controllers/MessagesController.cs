using System.ComponentModel.DataAnnotations;
using ChatWithMe.Api.Contracts;
using ChatWithMe.Api.Extensions;
using ChatWithMe.Api.Serialization;
using ChatWithMe.Application.Messages;
using ChatWithMe.Domain.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessagesController(IMessageAppService messageAppService, TimeZoneInfo timeZone) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MessageDto>> SendAsync([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var senderId = User.GetUserId();

        if (senderId is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "身份验证失败",
                Detail = "访问令牌缺少可用的 sub 声明。",
            });
        }

        var message = await messageAppService.SendAsync(senderId.Value, request, cancellationToken);

        return Created("/api/messages/" + message.Id, message);
    }

    /// <param name="before">
    /// 翻页游标，取上一页返回的 nextBefore 原值，形如 <c>yyyy/MM/dd HH:mm:ss.fff|{messageId}</c>
    /// （配置时区口径，时间保留毫秒，Id 为同毫秒消息的次序兜底）。这里按字符串接收再显式解析，
    /// 避免 query 绑定走服务器区域设置，把 UTC+8 文本当成 UTC 或当前区域时间比较。
    /// 也接受不带 Id 的旧式时间戳游标。
    /// </param>
    [HttpGet]
    public async Task<ActionResult<MessagePageResponse>> GetAsync(
        [FromQuery] Guid conversationId,
        [FromQuery] string? before = null,
        [FromQuery, Range(1, MessageAppService.MaxPageSize)] int limit = MessageAppService.DefaultPageSize,
        CancellationToken cancellationToken = default)
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

        MessageCursor? beforeCursor = null;

        if (!string.IsNullOrWhiteSpace(before))
        {
            if (!MessageCursorCodec.TryDecode(before, timeZone, out beforeCursor))
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "翻页游标不合法",
                    Detail = "before 需为 " + LocalDateTimeConverter.CursorFormat + "|{messageId}（上一页的 nextBefore）或 " +
                             LocalDateTimeConverter.CursorFormat + " 或 " + LocalDateTimeConverter.Format + " 格式（配置时区口径），收到：" + before,
                });
            }
        }

        var page = await messageAppService.GetAsync(userId.Value, conversationId, beforeCursor, limit, cancellationToken);
        var nextBefore = page.Next is null ? null : MessageCursorCodec.Encode(page.Next, timeZone);

        return Ok(new MessagePageResponse(page.Items, nextBefore));
    }
}
