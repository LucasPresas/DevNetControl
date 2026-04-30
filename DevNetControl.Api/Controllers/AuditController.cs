using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Dtos; // IMPORTANTE: Agrega esto
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
    public async Task<IActionResult> GetAuditHistory()
    {
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        // Ahora SourceUser y TargetUser existen y podemos incluirlos
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
}