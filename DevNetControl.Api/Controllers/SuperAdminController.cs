using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[superadmin]")]
[Authorize(Policy = "SuperAdminOnly")]
public class SuperAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuperAdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetAllTenants()
    {
        var tenants = await _context.Tenants
            .Select(t => new {
                t.Id,
                t.Name,
                t.Subdomain,
                t.AdminEmail,
                t.IsActive,
                t.CreatedAt,
                UserCount = t.Users.Count(),
                NodeCount = t.VpsNodes.Count()
            })
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        if (await _context.Tenants.AnyAsync(t => t.Subdomain == request.Subdomain))
            return BadRequest("El subdominio ya esta en uso.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Subdomain = request.Subdomain,
            AdminEmail = request.AdminEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserName = request.AdminUsername,
            PasswordHash = BC.HashPassword(request.AdminPassword),
            Role = UserRole.Admin,
            Credits = 10000
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Tenant creado exitosamente", TenantId = tenant.Id, AdminUserId = admin.Id });
    }

    [HttpPut("tenants/{id}/toggle")]
    public async Task<IActionResult> ToggleTenant(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound("Tenant no encontrado.");

        tenant.IsActive = !tenant.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Tenant {(tenant.IsActive ? "activado" : "desactivado")}" });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetGlobalStats()
    {
        var totalTenants = await _context.Tenants.CountAsync();
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive);
        var totalUsers = await _context.Users.CountAsync();
        var totalNodes = await _context.VpsNodes.CountAsync();
        var totalTransactions = await _context.CreditTransactions.CountAsync();

        return Ok(new {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            TotalUsers = totalUsers,
            TotalNodes = totalNodes,
            TotalTransactions = totalTransactions
        });
    }
}

public record CreateTenantRequest(string Name, string Subdomain, string AdminEmail, string AdminUsername, string AdminPassword);
