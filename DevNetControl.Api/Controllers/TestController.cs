using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("check-admin")]
    public async Task<IActionResult> CheckAdmin()
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");
        
        if (admin == null)
            return NotFound("El admin no fue creado en la DB.");

        return Ok(new { 
            Status = "Todo OK", 
            User = admin.UserName, 
            Role = admin.Role.ToString(),
            Credits = admin.Credits 
        });
    }
}