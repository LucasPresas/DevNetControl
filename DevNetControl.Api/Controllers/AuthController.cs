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
}

// El DTO (Data Transfer Object) para recibir el login
public record LoginRequest(string UserName, string Password);