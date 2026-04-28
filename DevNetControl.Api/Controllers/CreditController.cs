using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevNetControl.Api.Infrastructure.Services;
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

        if (!result.Success) return BadRequest(result.Message);

        return Ok(new { Message = result.Message });
    }
}

public record TransferRequest(Guid ToUserId, decimal Amount);