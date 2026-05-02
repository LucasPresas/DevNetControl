using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditController : ControllerBase
{
    private readonly CreditService _creditService;
    private readonly ApplicationDbContext _context;

    public CreditController(CreditService creditService, ApplicationDbContext context)
    {
        _creditService = creditService;
        _context = context;
    }

    [HttpPost("transfer")]
    [RateLimit("credit-transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var sourceId = ClaimsHelper.GetCurrentUserId(User);
        // Usamos ToUserId para que coincida con el DTO
        var result = await _creditService.TransferCreditsAsync(sourceId, request.ToUserId, request.Amount);
        return result.Success ? Ok(result) : BadRequest(new { Message = result.Message });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var balance = await _creditService.GetBalanceAsync(userId);
        return Ok(new { Balance = balance });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
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
}