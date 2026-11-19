using ChatWithMe.Api.Auth;
using ChatWithMe.Api.Extensions;
using ChatWithMe.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatWithMe.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Policy = AuthPolicies.ChatUser)]
public sealed class AnnouncementsController(AnnouncementAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await service.ListAsync(page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await service.GetAsync(id, cancellationToken));
}

[ApiController]
[Route("api/admin/notifications")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AdminAnnouncementsController(AnnouncementAppService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await service.ListAsync(page, pageSize, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Publish(PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        if (adminId is null) return Unauthorized();
        var result = await service.PublishAsync(adminId.Value, request, cancellationToken);
        return Created("/api/notifications/" + result.Notification.Id, result);
    }
}
