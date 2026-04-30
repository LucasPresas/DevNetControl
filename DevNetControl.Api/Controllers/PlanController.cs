using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlanController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PlanController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPlans()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var isAdmin = role == "Admin" || role == "SuperAdmin";

        IQueryable<Plan> plansQuery = _context.Plans
            .Include(p => p.Users)
            .Where(p => p.TenantId == tenantId);

        if (!isAdmin)
        {
            plansQuery = plansQuery.Where(p =>
                p.AllowedUsers.Any(pa => pa.UserId == userId) ||
                p.CreditCost == 0);
        }

        var plans = await plansQuery
            .OrderBy(p => p.CreditCost)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.DurationHours,
                p.CreditCost,
                p.MaxConnections,
                p.MaxDevices,
                p.IsActive,
                p.IsTrial,
                UsersCount = isAdmin ? p.Users.Count(u => u.TenantId == tenantId) : 0
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpGet("my-plans")]
    public async Task<IActionResult> GetMyPlans()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var isAdmin = role == "Admin" || role == "SuperAdmin";

        if (isAdmin)
        {
            var allPlans = await _context.Plans
                .Where(p => p.TenantId == tenantId && p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.DurationHours,
                    p.CreditCost,
                    p.MaxConnections,
                    p.MaxDevices,
                    p.IsTrial
                })
                .OrderBy(p => p.CreditCost)
                .ToListAsync();

            return Ok(allPlans);
        }

        var myPlans = await _context.Plans
            .Where(p => p.TenantId == tenantId && p.IsActive &&
                (p.AllowedUsers.Any(pa => pa.UserId == userId) || p.CreditCost == 0))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.DurationHours,
                p.CreditCost,
                p.MaxConnections,
                p.MaxDevices,
                p.IsTrial
            })
            .OrderBy(p => p.CreditCost)
            .ToListAsync();

        return Ok(myPlans);
    }

    [HttpPost("user/{userId}/plans")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserPlanAccess(Guid userId, [FromBody] UpdateUserPlanAccessRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado." });

        var existingAccess = await _context.PlanAccesses.Where(pa => pa.UserId == userId).ToListAsync();
        _context.PlanAccesses.RemoveRange(existingAccess);

        if (request.PlanIds != null)
        {
            foreach (var planId in request.PlanIds)
            {
                var plan = await _context.Plans.FindAsync(planId);
                if (plan != null && plan.TenantId == tenantId)
                {
                    _context.PlanAccesses.Add(new PlanAccess
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        PlanId = planId,
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Planes actualizados." });
    }

    [HttpGet("user/{userId}/plans")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetUserPlanAccess(Guid userId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado." });

        var plans = await _context.PlanAccesses
            .Include(pa => pa.Plan)
            .Where(pa => pa.UserId == userId)
            .Select(pa => new {
                pa.Plan.Id,
                pa.Plan.Name,
                pa.Plan.DurationHours,
                pa.Plan.CreditCost,
                pa.Plan.MaxDevices,
                pa.Plan.IsTrial
            })
            .ToListAsync();

        return Ok(plans);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DurationHours = request.DurationHours,
            CreditCost = request.CreditCost,
            MaxConnections = request.MaxConnections,
            MaxDevices = request.MaxDevices,
            IsActive = true
        };

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Plan creado exitosamente", PlanId = plan.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (plan == null)
            return NotFound(new { Message = "Plan no encontrado." });

        if (!string.IsNullOrEmpty(request.Name))
            plan.Name = request.Name;

        if (request.Description != null)
            plan.Description = request.Description;

        if (request.DurationHours.HasValue)
            plan.DurationHours = request.DurationHours.Value;

        if (request.CreditCost.HasValue)
            plan.CreditCost = request.CreditCost.Value;

        if (request.MaxConnections.HasValue)
            plan.MaxConnections = request.MaxConnections.Value;

        if (request.MaxDevices.HasValue)
            plan.MaxDevices = request.MaxDevices.Value;

        if (request.IsActive.HasValue)
            plan.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Plan actualizado correctamente." });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeletePlan(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var plan = await _context.Plans
            .Include(p => p.Users)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (plan == null)
            return NotFound(new { Message = "Plan no encontrado." });

        if (plan.Users.Any())
            return BadRequest(new { Message = "No se puede eliminar un plan con usuarios asignados." });

        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Plan eliminado correctamente." });
    }

    [HttpPost("{id}/toggle")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> TogglePlan(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
        if (plan == null)
            return NotFound(new { Message = "Plan no encontrado." });

        plan.IsActive = !plan.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Plan {(plan.IsActive ? "activado" : "desactivado")}" });
    }

    [HttpGet("available")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailablePlans()
    {
        var plans = await _context.Plans
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.DurationHours,
                p.CreditCost,
                p.MaxConnections,
                p.MaxDevices,
                p.IsTrial
            })
            .OrderBy(p => p.CreditCost)
            .ToListAsync();

        return Ok(plans);
    }
}

public record CreatePlanRequest(string Name, string? Description, int DurationHours, decimal CreditCost, int MaxConnections = 1, int MaxDevices = 1);
public record UpdatePlanRequest(string? Name = null, string? Description = null, int? DurationHours = null, decimal? CreditCost = null, int? MaxConnections = null, int? MaxDevices = null, bool? IsActive = null);
public record UpdateUserPlanAccessRequest(Guid[]? PlanIds);
