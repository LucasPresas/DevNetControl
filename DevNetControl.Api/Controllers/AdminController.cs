using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Dtos; // Importante para los nuevos DTOs centralizados
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")] // Aplicado a nivel de clase para mayor seguridad
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;
    private readonly NotificationService _notificationService;
    private readonly ActivityLogService _activityLogService;

    public AdminController(ApplicationDbContext context, CreditService creditService, NotificationService notificationService, ActivityLogService activityLogService)
    {
        _context = context;
        _creditService = creditService;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
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
            .Include(u => u.Parent)
            .Include(u => u.OwnedNodes)
            .Include(u => u.Plan)
            .Select(u => new {
                u.Id,
                u.UserName,
                Role = u.Role.ToString(),
                u.Credits,
                u.ParentId,
                ResellerName = u.Parent != null ? u.Parent.UserName : "N/A",
                u.IsActive,
                u.ServiceExpiry,
                MaxConnections = u.Plan != null ? u.Plan.MaxConnections : 0,
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
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado o fuera de su jurisdicción" });

        var changes = new List<string>();

        if (request.Role.HasValue)
        {
            changes.Add($"Rol: {user.Role} -> {request.Role.Value}");
            user.Role = request.Role.Value;
        }

        if (request.Credits.HasValue)
        {
            if (request.Credits.Value < 0) return BadRequest(new { Message = "Los créditos no pueden ser negativos" });
            changes.Add($"Créditos: {user.Credits} -> {request.Credits.Value}");
            user.Credits = request.Credits.Value;
        }

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _activityLogService.LogUserUpdatedAsync(
                actorUserId, user.Id, user.UserName,
                tenantId, actorRole, actorUserName, string.Join(", ", changes));
        }

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
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (targetUser == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        var creditsBefore = targetUser.Credits;
        
        var result = await _creditService.AddCreditsAsync(id, request.Amount, tenantId, actorUserId, actorRole, actorUserName);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        var creditsAfter = creditsBefore + request.Amount;

        await _activityLogService.LogCreditsLoadedAsync(
            actorUserId, id, targetUser.UserName,
            tenantId, actorRole, actorUserName,
            request.Amount, creditsBefore, creditsAfter);

        return Ok(new { Message = result.Message });
    }

    [HttpPost("users/{id}/add-connection")]
    public async Task<IActionResult> AddConnection(Guid id, [FromBody] AddConnectionRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var targetUser = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        
        if (targetUser == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        int connectionsToAdd = request.ConnectionsToAdd > 0 ? request.ConnectionsToAdd : 1;
        decimal creditCost = connectionsToAdd; // 1 credito por conexion

        if (targetUser.Credits < creditCost)
            return BadRequest(new { Message = $"Créditos insuficientes. Se requieren {creditCost} créditos." });

        var creditsBefore = targetUser.Credits;
        targetUser.Credits -= creditCost;
        targetUser.AdditionalConnections += connectionsToAdd;

        await _context.SaveChangesAsync();

        await _activityLogService.LogCreditsConsumedAsync(
            actorUserId, id, targetUser.UserName,
            tenantId, actorRole, actorUserName,
            creditCost, creditsBefore, targetUser.Credits,
            $"Agregadas {connectionsToAdd} conexión(es) a {targetUser.UserName}");

        return Ok(new { 
            Message = $"Se agregaron {connectionsToAdd} conexión(es). Créditos gastados: {creditCost}",
            NewMaxConnections = (targetUser.Plan?.MaxConnections ?? 0) + targetUser.AdditionalConnections,
            RemainingCredits = targetUser.Credits
        });
    }

    [HttpPost("users/{id}/renew-plan")]
    public async Task<IActionResult> RenewPlan(Guid id, [FromBody] RenewPlanRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var targetUser = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        
        if (targetUser == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        var plan = await _context.Plans.FindAsync(request.PlanId);
        if (plan == null)
            return BadRequest(new { Message = "Plan no encontrado" });

        if (targetUser.Credits < plan.CreditCost)
            return BadRequest(new { Message = $"Créditos insuficientes. Se requieren {plan.CreditCost} créditos." });

        var creditsBefore = targetUser.Credits;
        targetUser.Credits -= plan.CreditCost;
        targetUser.PlanId = plan.Id;
        targetUser.ServiceExpiry = DateTime.UtcNow.AddHours(request.DurationHours > 0 ? request.DurationHours : plan.DurationHours);

        await _context.SaveChangesAsync();

        await _activityLogService.LogCreditsConsumedAsync(
            actorUserId, id, targetUser.UserName,
            tenantId, actorRole, actorUserName,
            plan.CreditCost, creditsBefore, targetUser.Credits,
            $"Plan {plan.Name} renovado para {targetUser.UserName}. Duración: {request.DurationHours} horas");

        return Ok(new { 
            Message = $"Plan {plan.Name} renovado exitosamente. Expira: {targetUser.ServiceExpiry}",
            PlanName = plan.Name,
            ServiceExpiry = targetUser.ServiceExpiry,
            RemainingCredits = targetUser.Credits
        });
    }

    [HttpPut("users/{id}/update-basic")]
    public async Task<IActionResult> UpdateUserBasic(Guid id, [FromBody] UpdateUserBasicRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        var changes = new List<string>();

        if (!string.IsNullOrEmpty(request.UserName))
        {
            changes.Add($"Nombre: {user.UserName} -> {request.UserName}");
            user.UserName = request.UserName;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            changes.Add("Contraseña actualizada");
            user.PasswordHash = BC.HashPassword(request.Password);
        }

        if (request.ParentId.HasValue)
        {
            var newParent = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ParentId.Value && u.TenantId == tenantId);
            if (newParent == null)
                return BadRequest(new { Message = "El reseller especificado no existe en este tenant" });
            
            if (newParent.Role != UserRole.Reseller && newParent.Role != UserRole.SubReseller && newParent.Role != UserRole.Admin)
                return BadRequest(new { Message = "El usuario especificado no puede ser un reseller" });

            changes.Add($"Reseller: {user.ParentId} -> {request.ParentId.Value}");
            user.ParentId = request.ParentId.Value;
        }

        if (request.MaxConnections.HasValue)
        {
            changes.Add($"Conexiones adicionales: {user.AdditionalConnections} -> {request.MaxConnections.Value}");
            user.AdditionalConnections = request.MaxConnections.Value;
        }

        await _context.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await _activityLogService.LogUserUpdatedAsync(
                actorUserId, user.Id, user.UserName,
                tenantId, actorRole, actorUserName, string.Join(", ", changes));
        }

        return Ok(new { Message = "Usuario actualizado correctamente", Changes = changes });
    }
}