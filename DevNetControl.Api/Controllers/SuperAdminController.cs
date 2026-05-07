using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;

    public SuperAdminController(ApplicationDbContext context, EncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest req)
    {
        if (await _context.Tenants.AnyAsync(t => t.Subdomain == req.Subdomain))
            return BadRequest(new { Message = "El subdominio ya existe" });

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                Subdomain = req.Subdomain,
                AdminEmail = req.AdminEmail,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            var admin = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserName = req.AdminUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.AdminPassword),
                Role = UserRole.Admin,
                Credits = 9999999,
                IsActive = true
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            var plans = new List<Plan>
            {
                new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Basic", Description = "Plan básico", DurationHours = 720, CreditCost = 100, MaxConnections = 1, MaxDevices = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Pro", Description = "Plan profesional", DurationHours = 720, CreditCost = 250, MaxConnections = 5, MaxDevices = 3, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Enterprise", Description = "Plan empresarial", DurationHours = 720, CreditCost = 500, MaxConnections = 20, MaxDevices = 10, IsActive = true, CreatedAt = DateTime.UtcNow },
                new Plan { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Trial", Description = "Plan de prueba", DurationHours = 168, CreditCost = 0, MaxConnections = 1, MaxDevices = 1, IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            _context.Plans.AddRange(plans);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new { Message = "Tenant creado correctamente", TenantId = tenant.Id, AdminId = admin.Id });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = new
        {
            TotalTenants = await _context.Tenants.CountAsync(),
            ActiveTenants = await _context.Tenants.CountAsync(t => t.IsActive),
            TotalUsers = await _context.Users.CountAsync(),
            TotalNodes = await _context.VpsNodes.CountAsync()
        };

        return Ok(stats);
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _context.Tenants
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Subdomain,
                t.AdminEmail,
                t.IsActive,
                UserCount = _context.Users.Count(u => u.TenantId == t.Id),
                NodeCount = _context.VpsNodes.Count(v => v.TenantId == t.Id)
            })
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpPut("tenants/{id}/toggle")]
    public async Task<IActionResult> ToggleTenant(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound(new { Message = "Tenant no encontrado" });

        tenant.IsActive = !tenant.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Tenant {(tenant.IsActive ? "activado" : "desactivado")}", TenantId = tenant.Id, IsActive = tenant.IsActive });
    }

    [HttpPost("provision-node")]
    public async Task<IActionResult> ProvisionNode([FromBody] ProvisionNodeRequest req)
    {
        var superAdminId = ClaimsHelper.GetCurrentUserId(User);

        var node = new VpsNode
        {
            Id = Guid.NewGuid(),
            TenantId = req.TargetTenantId,
            IP = req.IpAddress,
            SshPort = req.SshPort,
            label = req.NodeName,
            EncryptedPassword = _encryption.Encrypt(req.Password),
            OwnerId = superAdminId,
        };

        _context.VpsNodes.Add(node);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Nodo asignado al cliente correctamente", NodeId = node.Id });
    }
}

public record CreateTenantRequest(string Name, string Subdomain, string AdminEmail, string AdminUsername, string AdminPassword);

public record ProvisionNodeRequest(Guid TargetTenantId, string NodeName, string IpAddress, string Password, int SshPort = 22);