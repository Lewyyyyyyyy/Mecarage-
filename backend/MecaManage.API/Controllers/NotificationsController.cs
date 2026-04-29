using MecaManage.Application.Features.Notifications.Queries;
using MecaManage.Application.Features.Notifications.Commands;
using MecaManage.API.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MecaManage.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("chef-inbox")]
    [Authorize(Roles = "ChefAtelier")]
    public async Task<IActionResult> GetChefInbox()
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new GetChefNotificationsQuery(userId));
        return Ok(result);
    }

    [HttpGet("my-notifications")]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new GetClientNotificationsQuery(userId));
        return Ok(result);
    }

    [HttpPatch("{notificationId}/mark-read")]
    public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
    {
        var userId = User.GetUserId();
        var result = await _mediator.Send(new MarkNotificationAsReadCommand(notificationId, userId));
        if (!result.Success)
            return NotFound(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

