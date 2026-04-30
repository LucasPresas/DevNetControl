using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SessionLogController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50, [FromQuery] string? search = null)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var query = _context.SessionLogs
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.Timestamp)
            .Take(limit)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.UserName.Contains(search) ||
                s.ClientIp.Contains(search) ||
                s.Action.Contains(search) ||
                s.Details.Contains(search));
        }

        var logs = await query
            .Select(s => new
            {
                s.Id,
                s.UserId,
                s.UserName,
                s.ClientIp,
                s.NodeIp,
                s.Action,
                s.Details,
                s.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetLogStats()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var today = DateTime.UtcNow.Date;

        var todayLogs = await _context.SessionLogs.CountAsync(s => s.TenantId == tenantId && s.Timestamp >= today);
        var activeUsers = await _context.SessionLogs
            .Where(s => s.TenantId == tenantId && s.Timestamp >= today)
            .Select(s => s.UserName)
            .Distinct()
            .CountAsync();

        return Ok(new
        {
            TodayLogs = todayLogs,
            ActiveUsersToday = activeUsers,
            Timestamp = DateTime.UtcNow
        });
    }
}
