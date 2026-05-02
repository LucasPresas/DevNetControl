using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Dtos;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserProvisioningService _provisioningService;
    private readonly CreditService _creditService;
    private readonly AuditService _auditService;
    private readonly BulkOperationService _bulkService;
    private readonly PlanValidationService _planValidationService;

    public UserController(ApplicationDbContext context, 
                          UserProvisioningService provisioningService,
                          CreditService creditService,
                          AuditService auditService,
                          BulkOperationService bulkService,
                          PlanValidationService planValidationService)
    {
        _context = context;
        _provisioningService = provisioningService;
        _creditService = creditService;
        _auditService = auditService;
        _bulkService = bulkService;
        _planValidationService = planValidationService;
    }

    #region Gestión de Usuarios y VPS

    [HttpPost("create")]
    [Authorize(Policy = "SubResellerOrAbove")]
    [RateLimit("user-create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var result = await _provisioningService.CreateUserAsync(
            parentId, tenantId, request.UserName, request.Password, request.PlanId, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        // Auditar creación de usuario
        await _auditService.LogAsync("UserCreated", 
            $"Usuario creado: {request.UserName} por {User.Identity?.Name}", 
            ClaimsHelper.GetCurrentUserId(User), ClaimsHelper.GetCurrentTenantId(User));

        return Ok(new { Message = result.Message, UserId = result.UserId });
    }

    [HttpPost("bulk-create")]
    [Authorize(Policy = "ResellerOrAbove")]
    [RateLimit("user-create")]
    public async Task<IActionResult> BulkCreateUsers([FromForm] BulkCreateUsersRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        if (request.CsvFile == null || request.CsvFile.Length == 0)
            return BadRequest(new { Message = "Debe proporcionar un archivo CSV." });

        using var stream = request.CsvFile.OpenReadStream();
        var (created, failed, errors) = await _bulkService.BulkCreateUsersAsync(parentId, tenantId, stream);

        return Ok(new
        {
            Message = $"Proceso completado: {created} creados, {failed} fallidos.",
            Created = created,
            Failed = failed,
            Errors = errors
        });
    }

    [HttpPost("{id}/extend-service")]
    public async Task<IActionResult> ExtendService(Guid id, [FromBody] ExtendServiceRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var targetUser = await _context.Users.FindAsync(id);
        if (targetUser == null || targetUser.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado." });

        // Validación de jerarquía: Solo Admin o el padre directo pueden extender
        if (role != "Admin" && role != "SuperAdmin" && targetUser.ParentId != userId)
            return Forbid();

        var result = await _provisioningService.ExtendServiceAsync(id, tenantId, request.Days, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
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

    #endregion

    #region Perfil y Jerarquía

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var user = await _context.Users
            .Include(u => u.Parent)
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return NotFound();

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            user.ServiceExpiry,
            user.IsProvisionedOnVps,
            Plan = user.Plan == null ? null : new { user.Plan.Name, user.Plan.MaxConnections },
            ParentName = user.Parent?.UserName
        });
    }

    [HttpGet("me/limits")]
    public async Task<IActionResult> GetMyLimits()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var (maxConnections, maxDevices, activeConnections, registeredDevices) = 
            await _planValidationService.GetUserLimitsAsync(userId);

        return Ok(new
        {
            MaxConnections = maxConnections,
            MaxDevices = maxDevices,
            ActiveConnections = activeConnections,
            RegisteredDevices = registeredDevices,
            Message = "Límites de tu plan."
        });
    }

    [HttpGet("me/hierarchy")]
    public async Task<IActionResult> GetMyHierarchy()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tree = await BuildHierarchyTreeAsync(userId);
        return Ok(tree);
    }

    [HttpGet("my-subusers")]
    public async Task<IActionResult> GetMySubUsers()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var subUsers = await _context.Users
            .Where(u => u.ParentId == parentId)
            .Select(u => new {
                u.Id, u.UserName, u.Credits, u.ServiceExpiry, u.IsActive,
                PlanName = u.Plan != null ? u.Plan.Name : "Sin Plan"
            }).ToListAsync();

        return Ok(subUsers);
    }

    #endregion

    #region Operaciones de Reseller (Admin Only)

    [HttpPost("create-reseller")]
    [Authorize(Policy = "AdminOnly")]
    [RateLimit("user-create")]
    public async Task<IActionResult> CreateReseller([FromBody] CreateResellerRequest request)
    {
        var adminId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.TenantId == tenantId))
            return BadRequest("El usuario ya existe.");

        // Lógica de validación de costos de planes simplificada
        var plans = await _context.Plans.Where(p => request.PlanIds.Contains(p.Id)).ToListAsync();
        decimal totalCost = plans.Sum(p => p.CreditCost);

        var admin = await _context.Users.FindAsync(adminId);
        if (admin == null) return NotFound("Admin no encontrado.");
        if (admin.Credits < totalCost) return BadRequest("Créditos insuficientes para asignar estos planes.");

        var reseller = new User {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.UserName,
            PasswordHash = BC.HashPassword(request.Password),
            Role = request.IsSubReseller ? UserRole.SubReseller : UserRole.Reseller,
            ParentId = adminId,
            Credits = request.InitialCredits,
            IsActive = true
        };

        _context.Users.Add(reseller);
        
        // Registro de Auditoría Unificado (Source/Target)
        if (totalCost > 0) {
            admin.Credits -= totalCost;
            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SourceUserId = adminId,
                TargetUserId = reseller.Id,
                Amount = totalCost,
                Type = CreditTransactionType.PlanPurchase,
                CreatedAt = DateTime.UtcNow,
                Note = $"Compra de planes para reseller {reseller.UserName}"
            });
        }

        await _context.SaveChangesAsync();

        // Auditar creación de reseller
        await _auditService.LogAsync("ResellerCreated", 
            $"Reseller creado: {request.UserName} por {User.Identity?.Name}", 
            ClaimsHelper.GetCurrentUserId(User), ClaimsHelper.GetCurrentTenantId(User));

        return Ok(new { Message = "Reseller creado", UserId = reseller.Id });
    }

    [HttpPost("{id}/load-credits")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> LoadCredits(Guid id, [FromBody] LoadCreditsRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        // Delegamos al servicio de créditos que ya maneja la auditoría correctamente
        var result = await _creditService.AddCreditsAsync(id, request.Amount, tenantId);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    #endregion

    #region Operaciones Masivas (Bulk)

    [HttpPost("bulk/extend-service")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> BulkExtendService([FromBody] BulkExtendServiceRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var (successCount, failCount, errors) = await _bulkService.BulkExtendServiceAsync(
            parentId, tenantId, request.UserIds, request.Days);

        return Ok(new
        {
            Message = $"Proceso completado: {successCount} extendidos, {failCount} fallidos.",
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors
        });
    }

    [HttpPost("bulk/delete")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var (successCount, failCount, errors) = await _bulkService.BulkDeleteAsync(
            parentId, tenantId, request.UserIds);

        return Ok(new
        {
            Message = $"Proceso completado: {successCount} eliminados, {failCount} fallidos.",
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors
        });
    }

    [HttpGet("my-resellers")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetMyResellers()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var resellers = await _context.Users
            .Where(u => (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller) &&
                          (u.ParentId == parentId || u.TenantId == tenantId))
            .Select(u => new
            {
                u.Id,
                u.UserName,
                Role = u.Role.ToString(),
                u.Credits,
                u.IsActive,
                u.ServiceExpiry,
                Plans = u.PlanAccesses.Select(pa => new { pa.Plan.Id, pa.Plan.Name, pa.Plan.IsTrial }).ToList(),
                Nodes = u.NodeAccesses.Select(na => na.Node.label).ToList(),
                UserCount = _context.Users.Count(c => c.ParentId == u.Id)
            })
            .ToListAsync();

        return Ok(resellers);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserBasicRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        if (!string.IsNullOrEmpty(request.UserName)) user.UserName = request.UserName;
        if (!string.IsNullOrEmpty(request.Password)) user.PasswordHash = BC.HashPassword(request.Password);

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Usuario actualizado" });
    }

    [HttpPost("{id}/toggle-suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleSuspend(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return Ok(new { Message = user.IsActive ? "Usuario activado" : "Usuario suspendido", IsActive = user.IsActive });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        // Eliminar subordinados y accesos
        var subordinates = _context.Users.Where(u => u.ParentId == id);
        _context.Users.RemoveRange(subordinates);
        _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == id));
        _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == id));

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Usuario eliminado" });
    }

    [HttpPost("{id}/nodes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserNodes(Guid id, [FromBody] UpdateUserNodesRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        // Eliminar accesos existentes
        var existing = _context.NodeAccesses.Where(na => na.UserId == id);
        _context.NodeAccesses.RemoveRange(existing);

        // Agregar nuevos accesos
        foreach (var nodeId in request.NodeIds)
        {
            var node = await _context.VpsNodes.FindAsync(nodeId);
            if (node != null && node.TenantId == tenantId)
            {
                _context.NodeAccesses.Add(new NodeAccess { Id = Guid.NewGuid(), UserId = id, NodeId = nodeId });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Nodos actualizados" });
    }

    #endregion

    #region Helpers Privados

    private async Task<HierarchyNodeDto?> BuildHierarchyTreeAsync(Guid userId)
    {
        var user = await _context.Users
            .Select(u => new { u.Id, u.UserName, u.Role, u.Credits, u.ParentId })
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var children = await GetSubordinateChildrenAsync(userId);

        return new HierarchyNodeDto(user.Id, user.UserName, user.Role, user.Credits, children);
    }

    private async Task<List<HierarchyNodeDto>> GetSubordinateChildrenAsync(Guid parentId)
    {
        var subs = await _context.Users
            .Where(u => u.ParentId == parentId)
            .Select(u => new { u.Id, u.UserName, u.Role, u.Credits })
            .ToListAsync();

        var nodes = new List<HierarchyNodeDto>();
        foreach (var s in subs)
        {
            nodes.Add(new HierarchyNodeDto(
                s.Id, s.UserName, s.Role, s.Credits, 
                await GetSubordinateChildrenAsync(s.Id) // Recursión async pura
            ));
        }
        return nodes;
    }

    #endregion
}