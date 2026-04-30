using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonitorController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SshService _sshService;

    public MonitorController(ApplicationDbContext context, SshService sshService)
    {
        _context = context;
        _sshService = sshService;
    }

    [HttpGet("user/{userId}/status")]
    public async Task<IActionResult> GetUserConnectionStatus(Guid userId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        
        // Buscamos al usuario y su plan para saber su límite
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        if (user == null || !user.IsProvisionedOnVps)
            return NotFound(new { Message = "Usuario no encontrado o sin VPS" });

        // Buscamos el nodo donde está alojado (esto depende de tu lógica de asignación)
        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.TenantId == tenantId); 
        if (node == null) return BadRequest("No hay nodo asignado al tenant");

        // Obtenemos sesiones reales del VPS
        var (count, pids) = await _sshService.GetActiveSessionsAsync(node.Id, user.UserName);

        return Ok(new 
        { 
            ActiveConnections = count, 
            MaxConnections = user.Plan?.MaxConnections ?? 1,
            Pids = pids
        });
    }

    [HttpPost("user/{userId}/enforce")]
    public async Task<IActionResult> EnforceLimits(Guid userId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        
        if (user == null || user.Plan == null) return NotFound();

        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.TenantId == tenantId);
        if (node == null) return BadRequest();

        // Ejecutamos el "Kick" si excede el límite
        bool kicked = await _sshService.EnforcementKickAsync(node.Id, user.UserName, user.Plan.MaxConnections);

        return Ok(new { 
            Success = true, 
            ActionTaken = kicked ? "Sesión excedida eliminada" : "Límites OK" 
        });
    }
}