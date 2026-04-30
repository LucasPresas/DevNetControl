using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
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

    // 1. EL TEST (Mantenemos el GET para chequear salud de la DB)
    [HttpGet("test-db")]
    public async Task<IActionResult> TestDatabase()
    {
        var userCount = await _context.Users.CountAsync();
        var adminExists = await _context.Users.AnyAsync(u => u.UserName == "admin");

        return Ok(new { 
            Message = "Servidor Activo", 
            Database = _context.Database.ProviderName,
            TotalUsers = userCount,
            AdminExists = adminExists
        });
    }

    // 2. EL LOGIN REAL (POST para enviar credenciales seguras)
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Buscamos al usuario
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.UserName);

        // Verificamos si existe y si el Hash de la clave coincide
        if (user == null || !BC.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });
        }

        // Generamos el "Ticket" de entrada (JWT)
        var token = _tokenService.GenerateToken(user);

        return Ok(new { 
            Token = token,
            User = user.UserName,
            Role = user.Role.ToString()
        });
    }

    // 3. CAMBIO DE CONTRASEÑA (Requiere autenticación)
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound("Usuario no encontrado");

        // Verificar contraseña actual
        if (!BC.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest("La contraseña actual es incorrecta");
        }

        // Validar nueva contraseña (mínimo 6 caracteres)
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            return BadRequest("La nueva contraseña debe tener al menos 6 caracteres");
        }

        // Actualizar contraseña
        user.PasswordHash = BC.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Contraseña actualizada correctamente" });
    }
}

// DTOs
public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);