using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;

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

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _context.Plans.FindAsync(id);
        if (plan == null) return NotFound();

        var role = ClaimsHelper.GetCurrentRole(User);
        // Ajustamos la lógica de borrado para que coincida con Guid.Empty
        if (role != "SuperAdmin" && plan.TenantId == Guid.Empty) return Forbid();

        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Plan eliminado" });
    }
}

public record PlanResponse(Guid Id, string Name, string Description, int DurationHours, decimal CreditCost, int MaxConnections, int MaxDevices, bool IsActive, bool IsTrial, int UsersCount);
public record CreatePlanRequest(string Name, string? Description, int DurationHours, decimal CreditCost, int MaxConnections = 1, int MaxDevices = 1, bool IsTrial = false);
public record UpdatePlanRequest(string? Name = null, string? Description = null, int? DurationHours = null, decimal? CreditCost = null, int? MaxConnections = null, int? MaxDevices = null, bool? IsActive = null);