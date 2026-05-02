using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.Services;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;

    public NotificationController(ApplicationDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMyNotifications(bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly, page, pageSize);
        return Ok(notifications);
    }

    [HttpPost("mark-read/{id}")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(new { Message = "Notificación marcada como leída." });
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(new { Count = count });
    }
}
