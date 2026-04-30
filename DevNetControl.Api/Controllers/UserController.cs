using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.Services;
using BC = BCrypt.Net.BCrypt;
using System.Security.Claims;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserProvisioningService _provisioningService;

    public UserController(ApplicationDbContext context, UserProvisioningService provisioningService)
    {
        _context = context;
        _provisioningService = provisioningService;
    }

    [HttpPost("create")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var result = await _provisioningService.CreateUserAsync(
            parentId, tenantId, request.UserName, request.Password, request.PlanId, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message, UserId = result.UserId });
    }

    [HttpPost("{id}/extend-service")]
    [Authorize]
    public async Task<IActionResult> ExtendService(Guid id, [FromBody] ExtendServiceRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var targetUser = await _context.Users.FindAsync(id);
        if (targetUser == null || targetUser.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && targetUser.ParentId != userId)
            return Forbid();

        var result = await _provisioningService.ExtendServiceAsync(id, tenantId, request.Days, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
    }

    [HttpPost("bulk/extend-service")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> BulkExtendService([FromBody] BulkExtendServiceRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        if (request.UserIds == null || request.UserIds.Length == 0)
            return BadRequest(new { Message = "No se seleccionaron usuarios" });

        var results = new List<object>();
        int successCount = 0;
        int failCount = 0;

        foreach (var targetId in request.UserIds)
        {
            var targetUser = await _context.Users.FindAsync(targetId);
            if (targetUser == null || targetUser.TenantId != tenantId)
            {
                results.Add(new { UserId = targetId, Success = false, Message = "Usuario no encontrado" });
                failCount++;
                continue;
            }

            if (role != "Admin" && role != "SuperAdmin" && targetUser.ParentId != userId)
            {
                results.Add(new { UserId = targetId, Success = false, Message = "Sin permisos" });
                failCount++;
                continue;
            }

            var result = await _provisioningService.ExtendServiceAsync(targetId, tenantId, request.Days, request.NodeId);
            results.Add(new { UserId = targetId, Success = result.Success, Message = result.Message });
            if (result.Success) successCount++; else failCount++;
        }

        return Ok(new {
            Message = $"{successCount} exitosos, {failCount} fallidos",
            SuccessCount = successCount,
            FailCount = failCount,
            Results = results
        });
    }

    [HttpPost("bulk/delete")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        if (request.UserIds == null || request.UserIds.Length == 0)
            return BadRequest(new { Message = "No se seleccionaron usuarios" });

        var results = new List<object>();
        int successCount = 0;
        int failCount = 0;

        foreach (var targetId in request.UserIds)
        {
            var targetUser = await _context.Users.FindAsync(targetId);
            if (targetUser == null || targetUser.TenantId != tenantId)
            {
                results.Add(new { UserId = targetId, Success = false, Message = "Usuario no encontrado" });
                failCount++;
                continue;
            }

            if (role != "Admin" && role != "SuperAdmin" && targetUser.ParentId != userId)
            {
                results.Add(new { UserId = targetId, Success = false, Message = "Sin permisos" });
                failCount++;
                continue;
            }

            if (targetUser.IsProvisionedOnVps)
            {
                var nodeAccess = await _context.NodeAccesses
                    .FirstOrDefaultAsync(na => na.UserId == targetId);

                if (nodeAccess != null)
                {
                    var removeResult = await _provisioningService.RemoveUserFromVpsAsync(targetId, tenantId, nodeAccess.NodeId);
                    if (!removeResult.Success)
                    {
                        results.Add(new { UserId = targetId, Success = false, Message = "Error removiendo del VPS" });
                        failCount++;
                        continue;
                    }
                }
            }

            _context.Users.Remove(targetUser);
            successCount++;
            results.Add(new { UserId = targetId, Success = true, Message = "Eliminado" });
        }

        await _context.SaveChangesAsync();

        return Ok(new {
            Message = $"{successCount} eliminados, {failCount} fallidos",
            SuccessCount = successCount,
            FailCount = failCount,
            Results = results
        });
    }

    [HttpPost("{id}/remove-from-vps")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemoveFromVps(Guid id, [FromBody] RemoveFromVpsRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var result = await _provisioningService.RemoveUserFromVpsAsync(id, tenantId, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
    }

    [HttpGet("my-subusers")]
    public async Task<IActionResult> GetMySubUsers()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var subUsers = await _context.Users
            .Include(u => u.Plan)
            .Where(u => u.ParentId == parentId)
            .Select(u => new {
                u.Id,
                u.UserName,
                Role = u.Role.ToString(),
                u.Credits,
                u.MaxDevices,
                u.ServiceExpiry,
                u.IsTrial,
                u.TrialExpiry,
                u.IsProvisionedOnVps,
                u.IsActive,
                PlanName = u.Plan != null ? u.Plan.Name : null,
                PlanDurationHours = u.Plan != null ? u.Plan.DurationHours : 0,
                IsTrialPlan = u.Plan != null && u.Plan.IsTrial,
                PlanCreditCost = u.Plan != null ? u.Plan.CreditCost : 0,
            })
            .ToListAsync();

        return Ok(subUsers);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);

        var user = await _context.Users
            .Include(u => u.Parent)
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            user.MaxDevices,
            user.ServiceExpiry,
            user.IsTrial,
            user.TrialExpiry,
            user.IsProvisionedOnVps,
            Plan = user.Plan == null ? null : new { user.Plan.Id, user.Plan.Name, user.Plan.MaxConnections, user.Plan.MaxDevices },
            Parent = user.Parent == null ? null : new { user.Parent.UserName }
        });
    }

    [HttpGet("me/hierarchy")]
    public async Task<IActionResult> GetMyHierarchy()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);

        var hierarchy = await BuildHierarchyTreeAsync(userId);

        return Ok(hierarchy);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentRole = ClaimsHelper.GetCurrentRole(User);

        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (currentRole != "Admin" && currentRole != "SuperAdmin" && user.ParentId != userId)
            return Forbid();

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            user.MaxDevices,
            user.ServiceExpiry,
            user.IsTrial,
            user.TrialExpiry,
            user.IsProvisionedOnVps,
            Plan = user.Plan == null ? null : new { user.Plan.Id, user.Plan.Name, user.Plan.MaxConnections, user.Plan.MaxDevices, user.Plan.CreditCost }
        });
    }

    [HttpGet("my-resellers")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetMyResellers()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var resellers = await _context.Users
            .Where(u => u.TenantId == tenantId && (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller))
            .ToListAsync();

        var result = new List<object>();
        foreach (var r in resellers)
        {
            var nodes = await _context.NodeAccesses
                .Include(na => na.Node)
                .Where(na => na.UserId == r.Id)
                .Select(na => na.Node.label)
                .ToListAsync();

            var plans = await _context.PlanAccesses
                .Include(pa => pa.Plan)
                .Where(pa => pa.UserId == r.Id)
                .Select(pa => new { pa.Plan.Name, pa.Plan.Id, pa.Plan.DurationHours, pa.Plan.CreditCost, pa.Plan.IsTrial })
                .ToListAsync();

            var primaryPlan = plans.FirstOrDefault();

            result.Add(new
            {
                r.Id,
                r.UserName,
                Role = r.Role.ToString(),
                r.Credits,
                r.MaxDevices,
                r.ServiceExpiry,
                r.IsTrial,
                r.IsProvisionedOnVps,
                r.IsActive,
                PlanName = primaryPlan?.Name,
                PlanId = primaryPlan?.Id,
                DurationHours = primaryPlan?.DurationHours ?? 0,
                IsTrialPlan = primaryPlan?.IsTrial ?? false,
                NodeCount = nodes.Count,
                Nodes = nodes,
                PlanCount = plans.Count,
                Plans = plans.Select(p => new { p.Id, p.Name, p.DurationHours, p.CreditCost, p.IsTrial }).ToList()
            });
        }

        return Ok(result);
    }

    [HttpPost("create-reseller")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CreateReseller([FromBody] CreateResellerRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.TenantId == tenantId))
            return BadRequest(new { Message = "El nombre de usuario ya existe." });

        int maxDevices = 1;
        decimal totalPlanCost = 0;
        var planNames = new List<string>();

        if (request.PlanIds != null && request.PlanIds.Length > 0)
        {
            foreach (var planId in request.PlanIds)
            {
                var plan = await _context.Plans.FindAsync(planId);
                if (plan != null && plan.TenantId == tenantId)
                {
                    if (plan.MaxDevices > maxDevices)
                        maxDevices = plan.MaxDevices;

                    if (plan.CreditCost > 0)
                    {
                        totalPlanCost += plan.CreditCost;
                        planNames.Add(plan.Name);
                    }
                }
            }
        }

        var adminUser = await _context.Users.FindAsync(parentId);
        if (adminUser != null && adminUser.Credits < totalPlanCost)
            return BadRequest(new { Message = $"No tienes suficientes creditos. Se necesitan {totalPlanCost} creditos para los planes." });

        var reseller = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.UserName,
            PasswordHash = BC.HashPassword(request.Password),
            Role = request.IsSubReseller ? UserRole.SubReseller : UserRole.Reseller,
            ParentId = parentId,
            Credits = request.InitialCredits,
            IsActive = true,
            MaxDevices = maxDevices,
        };

        _context.Users.Add(reseller);

        if (request.NodeIds != null && request.NodeIds.Length > 0)
        {
            foreach (var nodeId in request.NodeIds)
            {
                var node = await _context.VpsNodes.FindAsync(nodeId);
                if (node != null && node.TenantId == tenantId)
                {
                    _context.NodeAccesses.Add(new NodeAccess
                    {
                        Id = Guid.NewGuid(),
                        UserId = reseller.Id,
                        NodeId = nodeId,
                    });
                }
            }
        }

        if (request.PlanIds != null && request.PlanIds.Length > 0)
        {
            foreach (var planId in request.PlanIds)
            {
                var plan = await _context.Plans.FindAsync(planId);
                if (plan != null && plan.TenantId == tenantId)
                {
                    _context.PlanAccesses.Add(new PlanAccess
                    {
                        Id = Guid.NewGuid(),
                        UserId = reseller.Id,
                        PlanId = planId,
                    });
                }
            }
        }

        if (totalPlanCost > 0 && adminUser != null)
        {
            adminUser.Credits -= totalPlanCost;
            _context.CreditTransactions.Add(new CreditTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FromUserId = parentId,
                ToUserId = parentId,
                Amount = -totalPlanCost,
                Type = CreditTransactionType.PlanPurchase,
                Note = $"Planes asignados a reseller {request.UserName}: {string.Join(", ", planNames)}",
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Reseller creado exitosamente.", UserId = reseller.Id });
    }

    [HttpPost("{id}/load-credits")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> LoadCredits(Guid id, [FromBody] LoadCreditsRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var adminId = ClaimsHelper.GetCurrentUserId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        if (reseller.Role != UserRole.Reseller && reseller.Role != UserRole.SubReseller)
            return BadRequest(new { Message = "El usuario no es un reseller." });

        var adminUser = await _context.Users.FindAsync(adminId);
        if (adminUser != null && adminUser.Credits < request.Amount)
            return BadRequest(new { Message = "No tienes suficientes creditos." });

        reseller.Credits += request.Amount;

        if (adminUser != null)
        {
            adminUser.Credits -= request.Amount;
        }

        _context.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromUserId = adminId,
            ToUserId = id,
            Amount = request.Amount,
            Type = CreditTransactionType.AdminCredit,
            Note = $"Creditos cargados por admin a {reseller.UserName}",
        });

        await _context.SaveChangesAsync();

        return Ok(new { Message = $"{request.Amount} creditos cargados a {reseller.UserName}." });
    }

    [HttpPost("{id}/change-plan")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangePlanRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var adminId = ClaimsHelper.GetCurrentUserId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        var newPlan = await _context.Plans.FindAsync(request.PlanId);
        if (newPlan == null)
            return NotFound(new { Message = "Plan no encontrado." });

        var oldPlanId = reseller.PlanId;
        reseller.PlanId = request.PlanId;
        reseller.MaxDevices = newPlan.MaxDevices;

        if (newPlan.CreditCost > 0)
        {
            var adminUser = await _context.Users.FindAsync(adminId);
            if (adminUser != null && adminUser.Credits < newPlan.CreditCost)
                return BadRequest(new { Message = "No tienes suficientes creditos para este plan." });

            if (adminUser != null)
            {
                adminUser.Credits -= newPlan.CreditCost;
                _context.CreditTransactions.Add(new CreditTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FromUserId = adminId,
                    ToUserId = id,
                    Amount = -newPlan.CreditCost,
                    Type = CreditTransactionType.PlanPurchase,
                    Note = $"Cambio de plan a {newPlan.Name} para {reseller.UserName}",
                });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Plan cambiado a {newPlan.Name}." });
    }

    [HttpPost("{id}/toggle-suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleSuspend(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        reseller.IsActive = !reseller.IsActive;
        await _context.SaveChangesAsync();

        var status = reseller.IsActive ? "activado" : "suspendido";
        return Ok(new { Message = $"Reseller {status} exitosamente.", IsActive = reseller.IsActive });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteReseller(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        var nodeAccesses = await _context.NodeAccesses.Where(na => na.UserId == id).ToListAsync();
        _context.NodeAccesses.RemoveRange(nodeAccesses);

        var subUsers = await _context.Users.Where(u => u.ParentId == id).ToListAsync();
        foreach (var sub in subUsers)
        {
            var subNodeAccesses = await _context.NodeAccesses.Where(na => na.UserId == sub.Id).ToListAsync();
            _context.NodeAccesses.RemoveRange(subNodeAccesses);
        }
        _context.Users.RemoveRange(subUsers);

        _context.Users.Remove(reseller);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Reseller y sus usuarios eliminados." });
    }

    [HttpGet("{id}/nodes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetResellerNodes(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        var nodes = await _context.NodeAccesses
            .Include(na => na.Node)
            .Where(na => na.UserId == id)
            .Select(na => new {
                na.Node.Id,
                na.Node.IP,
                na.Node.SshPort,
                na.Node.label,
            })
            .ToListAsync();

        return Ok(nodes);
    }

    [HttpPost("{id}/nodes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateResellerNodes(Guid id, [FromBody] UpdateResellerNodesRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var reseller = await _context.Users.FindAsync(id);
        if (reseller == null || reseller.TenantId != tenantId)
            return NotFound(new { Message = "Reseller no encontrado." });

        var existingAccess = await _context.NodeAccesses.Where(na => na.UserId == id).ToListAsync();
        _context.NodeAccesses.RemoveRange(existingAccess);

        if (request.NodeIds != null)
        {
            foreach (var nodeId in request.NodeIds)
            {
                var node = await _context.VpsNodes.FindAsync(nodeId);
                if (node != null && node.TenantId == tenantId)
                {
                    _context.NodeAccesses.Add(new NodeAccess
                    {
                        Id = Guid.NewGuid(),
                        UserId = id,
                        NodeId = nodeId,
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Nodos actualizados." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateMyUserRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentRole = ClaimsHelper.GetCurrentRole(User);

        if (currentRole != "Admin" && currentRole != "SuperAdmin" && id != userId)
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null || user.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.Id != id && u.TenantId == tenantId))
                return BadRequest("El nombre de usuario ya existe.");

            user.UserName = request.UserName;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BC.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Perfil actualizado correctamente" });
    }

    private async Task<HierarchyNodeDto> BuildHierarchyTreeAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new KeyNotFoundException("Usuario no encontrado");

        return new HierarchyNodeDto(
            user.Id,
            user.UserName,
            user.Role.ToString(),
            user.Credits,
            user.Subordinates.Select(s => new HierarchyNodeDto(
                s.Id,
                s.UserName,
                s.Role.ToString(),
                s.Credits,
                GetSubordinateChildren(s.Id).Result
            )).ToList()
        );
    }

    private async Task<List<HierarchyNodeDto>> GetSubordinateChildren(Guid parentId)
    {
        var children = await _context.Users
            .Include(u => u.Subordinates)
            .Where(u => u.ParentId == parentId)
            .ToListAsync();

        return children.Select(c => new HierarchyNodeDto(
            c.Id,
            c.UserName,
            c.Role.ToString(),
            c.Credits,
            GetSubordinateChildren(c.Id).Result
        )).ToList();
    }
}

public record CreateUserRequest(string UserName, string Password, Guid PlanId, Guid? NodeId = null);
public record ExtendServiceRequest(int Days, Guid? NodeId = null);
public record RemoveFromVpsRequest(Guid NodeId);
public record UpdateMyUserRequest(string? UserName = null, string? Password = null);
public record HierarchyNodeDto(Guid Id, string UserName, string Role, decimal Credits, List<HierarchyNodeDto> Children);
public record BulkExtendServiceRequest(Guid[] UserIds, int Days, Guid? NodeId = null);
public record BulkDeleteRequest(Guid[] UserIds);
public record CreateResellerRequest(string UserName, string Password, bool IsSubReseller, Guid[]? PlanIds = null, decimal InitialCredits = 0, Guid[]? NodeIds = null);
public record LoadCreditsRequest(decimal Amount);
public record ChangePlanRequest(Guid PlanId);
public record UpdateResellerNodesRequest(Guid[]? NodeIds);
