using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Dtos;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlanController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var plans = await _context.Plans
            .OrderBy(p => p.CreditCost)
            .Select(p => new PlanResponse(
                p.Id, p.Name, p.Description, p.DurationHours, p.CreditCost, 
                p.MaxConnections, p.MaxDevices, p.IsActive, p.IsTrial,
                p.Users.Count
            ))
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create([FromBody] CreatePlanRequest req)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            // CORRECCIÓN CS0037: Si es SuperAdmin, usamos un Guid vacío o el ID del sistema 
            // para representar lo "Global", ya que tu modelo Plan.TenantId no es nullable.
            TenantId = (role == "SuperAdmin") ? Guid.Empty : tenantId,
            Name = req.Name,
            Description = req.Description ?? "",
            DurationHours = req.DurationHours,
            CreditCost = req.CreditCost,
            MaxConnections = req.MaxConnections,
            MaxDevices = req.MaxDevices,
            IsActive = true
            // Nota: Se eliminó IsTrial de aquí porque tu modelo parece ser de solo lectura
        };

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Plan creado exitosamente", PlanId = plan.Id });
    }

    [HttpGet("my-plans")]
    public async Task<IActionResult> GetMyPlans()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var plans = await _context.Plans
            .Where(p => p.IsActive && (p.TenantId == tenantId || p.TenantId == Guid.Empty || role == "SuperAdmin"))
            .OrderBy(p => p.CreditCost)
            .Select(p => new PlanResponse(
                p.Id, p.Name, p.Description, p.DurationHours, p.CreditCost,
                p.MaxConnections, p.MaxDevices, p.IsActive, p.IsTrial,
                p.Users.Count
            ))
            .ToListAsync();

        return Ok(plans);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null) return NotFound();
        
        var role = ClaimsHelper.GetCurrentRole(User);
        if (role != "SuperAdmin" && plan.TenantId == Guid.Empty) return Forbid();
        
        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Plan eliminado" });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null) return NotFound(new { Message = "Plan no encontrado." });
        
        var role = ClaimsHelper.GetCurrentRole(User);
        if (role != "SuperAdmin" && plan.TenantId == Guid.Empty) return Forbid();
        
        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description ?? "";
        if (request.DurationHours.HasValue) plan.DurationHours = request.DurationHours.Value;
        if (request.CreditCost.HasValue) plan.CreditCost = request.CreditCost.Value;
        if (request.MaxConnections.HasValue) plan.MaxConnections = request.MaxConnections.Value;
        if (request.MaxDevices.HasValue) plan.MaxDevices = request.MaxDevices.Value;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Plan actualizado" });
    }

    [HttpPost("{id}/toggle")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null) return NotFound(new { Message = "Plan no encontrado." });
        
        var role = ClaimsHelper.GetCurrentRole(User);
        if (role != "SuperAdmin" && plan.TenantId == Guid.Empty) return Forbid();
        
        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Estado del plan actualizado", IsActive = plan.IsActive });
    }

    [HttpGet("user/{userId}/plans")]
    public async Task<IActionResult> GetUserPlans(Guid userId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        var plans = await _context.PlanAccesses
            .Include(pa => pa.Plan)
            .Where(pa => pa.UserId == userId)
            .Select(pa => new { pa.Plan.Id, pa.Plan.Name, pa.Plan.IsTrial })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost("user/{userId}/plans")]
    public async Task<IActionResult> UpdateUserPlans(Guid userId, [FromBody] UpdateUserPlansRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        // Eliminar accesos existentes
        var existing = _context.PlanAccesses.Where(pa => pa.UserId == userId);
        _context.PlanAccesses.RemoveRange(existing);

        // Agregar nuevos accesos
        foreach (var planId in request.PlanIds)
        {
            var plan = await _context.Plans.FindAsync(planId);
            if (plan != null && (plan.TenantId == tenantId || plan.TenantId == Guid.Empty))
            {
                _context.PlanAccesses.Add(new PlanAccess { Id = Guid.NewGuid(), UserId = userId, PlanId = planId });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Planes actualizados" });
    }
}

public record PlanResponse(Guid Id, string Name, string Description, int DurationHours, decimal CreditCost, int MaxConnections, int MaxDevices, bool IsActive, bool IsTrial, int UsersCount);
public record CreatePlanRequest(string Name, string? Description, int DurationHours, decimal CreditCost, int MaxConnections = 1, int MaxDevices = 1, bool IsTrial = false);
public record UpdatePlanRequest(string? Name = null, string? Description = null, int? DurationHours = null, decimal? CreditCost = null, int? MaxConnections = null, int? MaxDevices = null, bool? IsActive = null);