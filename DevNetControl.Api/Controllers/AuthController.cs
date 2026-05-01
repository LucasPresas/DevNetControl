using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Dtos;
using DevNetControl.Api.Domain;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly TokenService _tokenService;
    private readonly AuditService _auditService;

    public AuthController(ApplicationDbContext context, TokenService tokenService, AuditService auditService)
    {
        _context = context;
        _tokenService = tokenService;
        _auditService = auditService;
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
    [RateLimit("auth-login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == request.UserName);

        if (user == null || !BC.Verify(request.Password, user.PasswordHash))
        {
            // Auditar login fallido
            await _auditService.LogAsync("LoginFailed", 
                $"Intento de login fallido para usuario: {request.UserName}", 
                null, user?.TenantId ?? Guid.Empty);
            return Unauthorized(new { Message = "Usuario o contraseña incorrectos" });
        }

        var tenant = await _context.Tenants.FindAsync(user.TenantId);
        if (tenant == null || !tenant.IsActive)
        {
            return Unauthorized(new { Message = "Tu organizacion esta desactivada. Contacta soporte." });
        }

        // Auditar login exitoso
        await _auditService.LogAsync("LoginSuccess", 
            $"Login exitoso para usuario: {user.UserName}", 
            user.Id, user.TenantId);

        var (accessToken, refreshToken) = _tokenService.GenerateTokens(user);

        // Guardar refresh token en BD
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = user.Id
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return Ok(new {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = user.UserName,
            Role = user.Role.ToString(),
            UserId = user.Id,
            Credits = user.Credits,
            TenantId = user.TenantId,
            TenantName = tenant.Name
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    [RateLimit("auth-change-password")]
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

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // Validar refresh token en BD
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
            return Unauthorized(new { Message = "Refresh token inválido o expirado" });

        // Marcar como usado
        storedToken.IsUsed = true;
        _context.RefreshTokens.Update(storedToken);

        // Generar nuevos tokens
        var (newAccessToken, newRefreshToken) = _tokenService.GenerateTokens(storedToken.User);

        // Guardar nuevo refresh token
        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            UserId = storedToken.User.Id
        });

        await _context.SaveChangesAsync();

        return Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
}

public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
