using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Dtos;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditController : ControllerBase
{
    private readonly CreditService _creditService;

    public CreditController(CreditService creditService) => _creditService = creditService;

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
}