using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;

namespace DevNetControl.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            var userCount = await _context.Users.CountAsync();
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");

            return Ok(new { 
                Message = "Servidor Activo", 
                Database = _context.Database.ProviderName,
                TotalUsers = userCount,
                AdminExists = adminUser != null
            });
        }
    }
}