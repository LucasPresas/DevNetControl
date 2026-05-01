using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Dtos;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditController(ApplicationDbContext context) => _context = context;

    [HttpGet("history")]
    public async Task<IActionResult> GetCreditHistory()
    {
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var transactions = await _context.CreditTransactions
            .Include(t => t.SourceUser)
            .Include(t => t.TargetUser)
            .Where(t => t.SourceUser.TenantId == tenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new CreditTransactionDto(
                t.Id,
                t.SourceUser.UserName,
                t.TargetUser != null ? t.TargetUser.UserName : "Sistema",
                t.Amount,
                t.Type.ToString(),
                t.SourceUserId == currentUserId ? "Sent" : "Received",
                t.CreatedAt
            ))
            .ToListAsync();

        return Ok(transactions);
    }

    [HttpGet("logs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAuditLogs(int page = 1, int pageSize = 50)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var logs = await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.TenantId == tenantId)
            .OrderByDescending(al => al.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => new 
            {
                al.Id,
                al.Action,
                al.Description,
                al.Timestamp,
                al.IpAddress,
                UserName = al.User != null ? al.User.UserName : "Sistema"
            })
            .ToListAsync();

        var total = await _context.AuditLogs
            .Where(al => al.TenantId == tenantId)
            .CountAsync();

        return Ok(new 
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Logs = logs
        });
    }
}