using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Domain;
using System.Security.Claims;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CreditController : ControllerBase
{
    private readonly CreditService _creditService;

    public CreditController(CreditService creditService)
    {
        _creditService = creditService;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var senderId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var result = await _creditService.TransferCreditsAsync(senderId, request.ToUserId, request.Amount);

        if (!result.Success) return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var balance = await _creditService.GetUserBalanceAsync(userId);
        
        return Ok(new { UserId = userId, Balance = balance });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var history = await _creditService.GetTransactionHistoryAsync(userId, 50);
        
        return Ok(history);
    }
}

public record TransferRequest(Guid ToUserId, decimal Amount);