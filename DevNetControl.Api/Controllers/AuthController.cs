using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(ApplicationDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestDatabase()
    {
        var userCount = await _context.Users.CountAsync();
        var adminExists = await _context.Users.AnyAsync(u => u.UserName == "admin");
        var tenantCount = await _context.Tenants.CountAsync();

        return Ok(new {
            Message = "Servidor Activo",
            Database = _context.Database.ProviderName,
            TotalUsers = userCount,
            TotalTenants = tenantCount,
            AdminExists = adminExists
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.UserName);

        if (user == null || !BC.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });
        }

        var tenant = await _context.Tenants.FindAsync(user.TenantId);
        if (tenant == null || !tenant.IsActive)
        {
            return Unauthorized(new { Message = "Tu organizacion esta desactivada. Contacta soporte." });
        }

        var token = _tokenService.GenerateToken(user);

        return Ok(new {
            Token = token,
            User = user.UserName,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            TenantName = tenant.Name
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("Usuario no encontrado");

        if (!BC.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest("La contraseña actual es incorrecta");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest("La nueva contraseña debe tener al menos 6 caracteres");
        }

        user.PasswordHash = BC.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Contraseña actualizada correctamente" });
    }
}

public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
