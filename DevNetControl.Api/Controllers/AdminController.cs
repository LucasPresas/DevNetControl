using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Dtos; // Importante para los nuevos DTOs centralizados

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")] // Aplicado a nivel de clase para mayor seguridad
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;
    private readonly NotificationService _notificationService;

    public AdminController(ApplicationDbContext context, CreditService creditService, NotificationService notificationService)
    {
        _context = context;
        _creditService = creditService;
        _notificationService = notificationService;
    }

    [HttpGet("dashboard-data")]
    public async Task<IActionResult> GetDashboardData()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);

        // Conteos por rol (dentro del tenant)
        var userStats = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        // Total créditos en el tenant
        var totalCredits = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .SumAsync(u => u.Credits);

        // Notificaciones sin leer para el admin
        var unreadNotifications = await _notificationService.GetUserNotificationsAsync(currentUserId, unreadOnly: true);

        // Nodos VPS en el tenant
        var nodesCount = await _context.VpsNodes
            .CountAsync(n => n.TenantId == tenantId || n.TenantId == Guid.Empty);

        return Ok(new {
            UserStats = userStats,
            TotalCredits = totalCredits,
            UnreadNotifications = unreadNotifications.Count,
            NodesCount = nodesCount,
            Message = "Datos del panel de control."
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var users = await _context.Users
            .Where(u => u.TenantId == tenantId)
            .Include(u => u.OwnedNodes)
            .Select(u => new {
                u.Id,
                u.UserName,
                Role = u.Role.ToString(),
                u.Credits,
                u.ParentId,
                SubordinatesCount = u.Subordinates.Count,
                NodesCount = u.OwnedNodes.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users
            .Include(u => u.Parent)
            .Include(u => u.Subordinates)
            .Include(u => u.OwnedNodes)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            Parent = user.Parent == null ? null : new { user.Parent.Id, user.Parent.UserName },
            Subordinates = user.Subordinates.Select(s => new { s.Id, s.UserName, s.Role }).ToList(),
            Nodes = user.OwnedNodes.Select(n => new { n.Id, n.IP, n.SshPort, Label = n.label })
        });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado o fuera de su jurisdicción" });

        if (request.Role.HasValue)
            user.Role = request.Role.Value;

        if (request.Credits.HasValue)
        {
            if (request.Credits.Value < 0) return BadRequest(new { Message = "Los créditos no pueden ser negativos" });
            user.Credits = request.Credits.Value;
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario actualizado correctamente" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        // Impedir el suicidio administrativo o borrar superiores
        if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
            return BadRequest(new { Message = "Restricción de seguridad: No se pueden eliminar cuentas administrativas de alto nivel" });

        if (user.Subordinates.Any())
            return BadRequest(new { Message = "El usuario tiene vendedores o clientes a cargo. Debe reasignarlos antes de eliminar la cuenta." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario eliminado correctamente" });
    }

    [HttpPost("users/{id}/add-credits")]
    public async Task<IActionResult> AddCredits(Guid id, [FromBody] AddCreditsRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        
        // Llamada corregida para usar la nueva firma de 3 argumentos
        var result = await _creditService.AddCreditsAsync(id, request.Amount, tenantId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
    }
}