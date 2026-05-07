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
    private readonly ActivityLogService _activityLogService;
    private readonly BulkOperationService _bulkService;
    private readonly PlanValidationService _planValidationService;
    private readonly UserOperationService _userOperationService;
    private readonly SshService _sshService;

    public UserController(ApplicationDbContext context, 
                          UserProvisioningService provisioningService,
                          CreditService creditService,
                          AuditService auditService,
                          ActivityLogService activityLogService,
                          BulkOperationService bulkService,
                          PlanValidationService planValidationService,
                          UserOperationService userOperationService,
                          SshService sshService)
    {
        _context = context;
        _provisioningService = provisioningService;
        _creditService = creditService;
        _auditService = auditService;
        _activityLogService = activityLogService;
        _bulkService = bulkService;
        _planValidationService = planValidationService;
        _userOperationService = userOperationService;
        _sshService = sshService;
    }

    #region Gestión de Usuarios y VPS

    [HttpPost("create")]
    [Authorize(Policy = "SubResellerOrAbove")]
    [RateLimit("user-create")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var parent = await _context.Users.FindAsync(parentId);
        var creditsBefore = parent?.Credits ?? 0;

        var plan = request.PlanId.HasValue ? await _context.Plans.FindAsync(request.PlanId.Value) : null;
        var planName = plan?.Name;

        var result = await _provisioningService.CreateUserAsync(
            parentId, tenantId, request.UserName, request.Password, request.PlanId, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        var parentAfter = await _context.Users.FindAsync(parentId);
        var creditsAfter = parentAfter?.Credits ?? 0;

        if (result.UserId.HasValue)
        {
            await _activityLogService.LogUserCreatedAsync(
                parentId, result.UserId.Value, request.UserName,
                tenantId, actorRole, actorUserName,
                creditsBefore, creditsAfter, request.PlanId, planName);
        }

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
        var userName = ClaimsHelper.GetCurrentUserName(User);

        Console.WriteLine($"[DEBUG] ExtendService - Target: {id}, Days: {request.Days}");

        var result = await _userOperationService.ExtendServiceAsync(
            id, tenantId, request.Days, request.NodeId);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        Console.WriteLine($"[DEBUG] Service extended. New expiry: {result.Message}");
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
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var subUsers = await _context.Users
            .Where(u => u.ParentId == parentId && u.Role != UserRole.Reseller && u.Role != UserRole.SubReseller)
            .Include(u => u.Plan)
            .Include(u => u.Parent)
            .Select(u => new {
                u.Id,
                u.UserName,
                u.Credits,
                u.ServiceExpiry,
                u.IsActive,
                Role = u.Role.ToString(),
                ResellerName = u.Parent != null ? u.Parent.UserName : "N/A",
                MaxConnections = u.Plan != null ? u.Plan.MaxConnections + u.AdditionalConnections : u.AdditionalConnections,
                MaxDevices = u.Plan != null ? u.Plan.MaxDevices : 1,
                PlanName = u.Plan != null ? u.Plan.Name : "Sin Plan",
                u.IsTrial,
                u.IsProvisionedOnVps,
                PlanDurationHours = u.Plan != null ? u.Plan.DurationHours : 0,
                u.AdditionalConnections,
                NodeLabel = u.IsProvisionedOnVps
                    ? _context.NodeAccesses
                        .Where(na => na.UserId == u.Id)
                        .Select(na => na.Node.label)
                        .FirstOrDefault()
                    : null
            }).ToListAsync();

        return Ok(subUsers);
    }

    [HttpGet("active-connections")]
    public async Task<IActionResult> GetActiveConnections()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var provisionedUsers = await _context.Users
            .Where(u => u.ParentId == parentId && u.IsProvisionedOnVps && u.IsActive)
            .ToListAsync();

        var nodeAccesses = await _context.NodeAccesses
            .Include(na => na.Node)
            .Where(na => provisionedUsers.Select(u => u.Id).Contains(na.UserId))
            .ToListAsync();

        var userNodeMap = nodeAccesses
            .GroupBy(na => na.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var activeConnections = new Dictionary<Guid, int>();

        foreach (var user in provisionedUsers)
        {
            if (userNodeMap.TryGetValue(user.Id, out var nodeAccess))
            {
                try
                {
                    var (count, _) = await _sshService.GetActiveSessionsAsync(nodeAccess.NodeId, user.UserName);
                    activeConnections[user.Id] = count;
                }
                catch
                {
                    activeConnections[user.Id] = 0;
                }
            }
            else
            {
                activeConnections[user.Id] = 0;
            }
        }

        return Ok(activeConnections);
    }

    [HttpGet("dashboard-stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var now = DateTime.UtcNow;
        var threeDaysFromNow = now.AddDays(3);

        var allSubUsers = await _context.Users
            .Where(u => u.ParentId == parentId && u.Role != UserRole.Reseller && u.Role != UserRole.SubReseller)
            .Include(u => u.Plan)
            .ToListAsync();

        int totalUsers = allSubUsers.Count;
        int activeUsers = allSubUsers.Count(u => u.ServiceExpiry.HasValue && u.ServiceExpiry.Value > now);
        int expiredUsers = allSubUsers.Count(u => u.ServiceExpiry.HasValue && u.ServiceExpiry.Value <= now);
        int expiringSoonUsers = allSubUsers.Count(u => u.ServiceExpiry.HasValue && u.ServiceExpiry.Value > now && u.ServiceExpiry.Value <= threeDaysFromNow);
        int trialUsers = allSubUsers.Count(u => u.IsTrial);
        int totalConnections = allSubUsers.Sum(u => u.Plan != null ? u.Plan.MaxConnections + u.AdditionalConnections : u.AdditionalConnections);

        int resellerCount = 0;
        if (actorRole == "Admin" || actorRole == "SuperAdmin")
        {
            resellerCount = await _context.Users.CountAsync(u => u.TenantId == tenantId && (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller));
        }
        else
        {
            resellerCount = await _context.Users.CountAsync(u => u.ParentId == parentId && (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller));
        }

        var provisionedUserIds = allSubUsers.Where(u => u.IsProvisionedOnVps && u.IsActive).Select(u => u.Id).ToList();
        int onlineUsers = 0;
        if (provisionedUserIds.Count > 0)
        {
            var nodeAccesses = await _context.NodeAccesses
                .Include(na => na.Node)
                .Where(na => provisionedUserIds.Contains(na.UserId))
                .ToListAsync();

            var userNodeMap = nodeAccesses
                .GroupBy(na => na.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var userId in provisionedUserIds)
            {
                if (userNodeMap.TryGetValue(userId, out var nodeAccess))
                {
                    try
                    {
                        var (count, _) = await _sshService.GetActiveSessionsAsync(nodeAccess.NodeId, allSubUsers.First(u => u.Id == userId).UserName);
                        if (count > 0) onlineUsers++;
                    }
                    catch { }
                }
            }
        }

        return Ok(new {
            totalUsers,
            activeUsers,
            expiredUsers,
            expiringSoonUsers,
            trialUsers,
            onlineUsers,
            resellerCount,
            totalConnections
        });
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
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.TenantId == tenantId))
            return BadRequest("El usuario ya existe.");

        var admin = await _context.Users.FindAsync(adminId);
        if (admin == null) return NotFound("Admin no encontrado.");

        var creditsBefore = admin.Credits;

        var plans = request.PlanIds != null && request.PlanIds.Count > 0
            ? await _context.Plans.Where(p => request.PlanIds.Contains(p.Id)).ToListAsync()
            : new List<Plan>();
        decimal totalCost = plans.Sum(p => p.CreditCost);

        if (admin.Credits < totalCost) return BadRequest("Créditos insuficientes para asignar estos planes.");

        var planNames = plans.Select(p => p.Name).ToList();

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
        
        if (totalCost > 0) {
            admin.Credits -= totalCost;
            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SourceUserId = adminId,
                SourceBalanceBefore = creditsBefore,
                SourceBalanceAfter = admin.Credits,
                TargetUserId = reseller.Id,
                TargetBalanceBefore = request.InitialCredits,
                TargetBalanceAfter = request.InitialCredits,
                Amount = totalCost,
                Type = CreditTransactionType.PlanPurchase,
                CreatedAt = DateTime.UtcNow,
                Note = $"Compra de planes para reseller {reseller.UserName}"
            });
        }

        await _context.SaveChangesAsync();

        var creditsAfter = admin.Credits;

        if (request.IsSubReseller)
        {
            await _activityLogService.LogSubResellerCreatedAsync(
                adminId, reseller.Id, request.UserName,
                tenantId, actorRole, actorUserName,
                creditsBefore, creditsAfter, request.InitialCredits, planNames);
        }
        else
        {
            await _activityLogService.LogResellerCreatedAsync(
                adminId, reseller.Id, request.UserName,
                tenantId, actorRole, actorUserName,
                creditsBefore, creditsAfter, request.InitialCredits, planNames);
        }

        await _auditService.LogAsync("ResellerCreated", 
            $"Reseller creado: {request.UserName} por {User.Identity?.Name}", 
            adminId, tenantId);

        return Ok(new { Message = "Reseller creado", UserId = reseller.Id });
    }

    [HttpPost("{id}/load-credits")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> LoadCredits(Guid id, [FromBody] LoadCreditsRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var result = await _creditService.AddCreditsAsync(id, request.Amount, tenantId, actorUserId, actorRole, actorUserName);
        
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id}/sub-reseller")]
    [Authorize(Policy = "ResellerOrAbove")]
    public async Task<IActionResult> DeleteSubReseller(Guid id)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        if (actorRole == "Admin" || actorRole == "SuperAdmin")
            return BadRequest("Los admins deben usar el endpoint de eliminacion principal.");

        var subReseller = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.ParentId == actorUserId);
        if (subReseller == null) return NotFound(new { Message = "Sub-reseller no encontrado o no pertenece al actor." });

        var creditsBefore = (await _context.Users.FindAsync(actorUserId))?.Credits ?? 0m;

        var deletedUserName = subReseller.UserName;
        var refundCredits = subReseller.Credits;

        // Collect all user IDs being deleted (sub-reseller + children)
        var childUserIds = _context.Users.Where(u => u.ParentId == id).Select(u => u.Id).ToList();
        var allDeletedIds = new List<Guid> { id };
        allDeletedIds.AddRange(childUserIds);

        // Remove child users' related records (ActivityLog restricts on ActorUserId)
        foreach (var childId in childUserIds)
        {
            _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == childId));
            _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == childId));
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs.Where(al => al.ActorUserId == childId || al.TargetUserId == childId));
            _context.Notifications.RemoveRange(_context.Notifications.Where(n => n.UserId == childId));
            _context.SessionLogs.RemoveRange(_context.SessionLogs.Where(sl => sl.UserId == childId));
        }
        _context.Users.RemoveRange(_context.Users.Where(u => u.ParentId == id));

        // Remove sub-reseller related records (ActivityLog restricts on ActorUserId)
        _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == id));
        _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == id));
        _context.ActivityLogs.RemoveRange(_context.ActivityLogs.Where(al => al.ActorUserId == id || al.TargetUserId == id));
        _context.Notifications.RemoveRange(_context.Notifications.Where(n => n.UserId == id));
        _context.SessionLogs.RemoveRange(_context.SessionLogs.Where(sl => sl.UserId == id));

        _context.Users.Remove(subReseller);

        if (refundCredits > 0)
        {
            var actor = await _context.Users.FindAsync(actorUserId);
            actor.Credits += refundCredits;
            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SourceUserId = subReseller.Id,
                SourceBalanceBefore = refundCredits,
                SourceBalanceAfter = 0,
                TargetUserId = actorUserId,
                TargetBalanceBefore = creditsBefore,
                TargetBalanceAfter = actor.Credits,
                Amount = refundCredits,
                Type = CreditTransactionType.Transfer,
                CreatedAt = DateTime.UtcNow,
                Note = $"Reembolso por eliminacion de sub-reseller {deletedUserName}"
            });

            await _activityLogService.LogSubResellerDeletedAsync(
                actorUserId, subReseller.Id, deletedUserName,
                tenantId, actorRole, actorUserName,
                refundCredits, creditsBefore, actor.Credits, childUserIds.Count);
        }
        else
        {
            await _activityLogService.LogSubResellerDeletedAsync(
                actorUserId, subReseller.Id, deletedUserName,
                tenantId, actorRole, actorUserName,
                0, creditsBefore, creditsBefore, childUserIds.Count);
        }

        return Ok(new { Message = "Sub-reseller eliminado", RefundedCredits = refundCredits });
    }

    [HttpPost("{id}/add-credits")]
    [Authorize(Policy = "ResellerOrAbove")]
    public async Task<IActionResult> AddCreditsToSubReseller(Guid id, [FromBody] LoadCreditsRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        if (actorRole == "Admin" || actorRole == "SuperAdmin")
            return BadRequest("Los admins deben usar el endpoint de carga principal.");

        if (request.Amount <= 0) return BadRequest("La cantidad debe ser mayor a 0.");

        var subReseller = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId && u.ParentId == actorUserId);
        if (subReseller == null) return NotFound(new { Message = "Sub-reseller no encontrado o no pertenece al actor." });

        var actor = await _context.Users.FindAsync(actorUserId);
        if (actor == null) return NotFound("Usuario actor no encontrado.");

        var creditsBefore = actor.Credits;
        if (actor.Credits < request.Amount)
            return BadRequest("Creditos insuficientes.");

        var targetBalanceBefore = subReseller.Credits;
        actor.Credits -= request.Amount;
        subReseller.Credits += request.Amount;

        _context.CreditTransactions.Add(new CreditTransaction {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceUserId = actorUserId,
            SourceBalanceBefore = creditsBefore,
            SourceBalanceAfter = actor.Credits,
            TargetUserId = subReseller.Id,
            TargetBalanceBefore = targetBalanceBefore,
            TargetBalanceAfter = subReseller.Credits,
            Amount = request.Amount,
            Type = CreditTransactionType.Transfer,
            CreatedAt = DateTime.UtcNow,
            Note = $"Carga de creditos para sub-reseller {subReseller.UserName}"
        });

        await _context.SaveChangesAsync();

        await _activityLogService.LogCreditsLoadedAsync(
            actorUserId, subReseller.Id, subReseller.UserName,
            tenantId, actorRole, actorUserName,
            request.Amount, targetBalanceBefore, subReseller.Credits);

        return Ok(new {
            Message = "Creditos cargados exitosamente",
            NewBalance = subReseller.Credits,
            ActorNewBalance = actor.Credits
        });
    }

    #endregion

    #region Operaciones Masivas (Bulk)

    [HttpPost("bulk/extend-service")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> BulkExtendService([FromBody] BulkExtendServiceRequest request)
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var (successCount, failCount, errors) = await _bulkService.BulkExtendServiceAsync(
            parentId, tenantId, request.UserIds, request.Days, parentId, actorRole, actorUserName);

        await _activityLogService.LogBulkOperationAsync(
            parentId, tenantId, actorRole, actorUserName,
            "BulkExtendService", request.UserIds.Count, successCount, failCount);

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
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        if (actorRole == "Admin" || actorRole == "SuperAdmin")
        {
            int successCount = 0;
            int failCount = 0;
            var errors = new List<string>();

            foreach (var id in request.UserIds)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
                if (user == null)
                {
                    failCount++;
                    errors.Add($"Usuario {id} no encontrado");
                    continue;
                }

                if (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin)
                {
                    failCount++;
                    errors.Add($"No se puede eliminar al usuario administrativo {user.UserName}");
                    continue;
                }

                _context.Users.RemoveRange(_context.Users.Where(u => u.ParentId == id));
                _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == id));
                _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == id));
                _context.Users.Remove(user);
                successCount++;
            }

            await _context.SaveChangesAsync();

            await _activityLogService.LogBulkOperationAsync(
                actorUserId, tenantId, actorRole, actorUserName,
                "BulkDelete", request.UserIds.Count, successCount, failCount);

            return Ok(new
            {
                Message = $"Proceso completado: {successCount} eliminados, {failCount} fallidos.",
                SuccessCount = successCount,
                FailCount = failCount,
                Errors = errors
            });
        }

        var (successCount2, failCount2, errors2) = await _bulkService.BulkDeleteAsync(
            actorUserId, tenantId, request.UserIds, actorUserId, actorRole, actorUserName);

        await _activityLogService.LogBulkOperationAsync(
            actorUserId, tenantId, actorRole, actorUserName,
            "BulkDelete", request.UserIds.Count, successCount2, failCount2);

        return Ok(new
        {
            Message = $"Proceso completado: {successCount2} eliminados, {failCount2} fallidos.",
            SuccessCount = successCount2,
            FailCount = failCount2,
            Errors = errors2
        });
    }

    [HttpPost("bulk/toggle-suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> BulkToggleSuspend([FromBody] BulkToggleSuspendRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        int successCount = 0;
        int failCount = 0;
        var errors = new List<string>();

        foreach (var id in request.UserIds)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
            if (user == null)
            {
                failCount++;
                errors.Add($"Usuario {id} no encontrado");
                continue;
            }

            user.IsActive = !user.IsActive;
            successCount++;

            await _activityLogService.LogUserSuspendedAsync(
                actorUserId, user.Id, user.UserName,
                tenantId, actorRole, actorUserName, !user.IsActive);
        }

        await _context.SaveChangesAsync();

        await _activityLogService.LogBulkOperationAsync(
            actorUserId, tenantId, actorRole, actorUserName,
            "BulkToggleSuspend", request.UserIds.Count, successCount, failCount);

        return Ok(new
        {
            Message = $"Proceso completado: {successCount} actualizados, {failCount} fallidos.",
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors
        });
    }

    [HttpGet("my-resellers")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> GetMyResellers()
    {
        var parentId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);

        IQueryable<User> query;

        if (actorRole == "Admin" || actorRole == "SuperAdmin")
        {
            query = _context.Users
                .Where(u => (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller) && u.TenantId == tenantId);
        }
        else
        {
            query = _context.Users
                .Where(u => (u.Role == UserRole.Reseller || u.Role == UserRole.SubReseller) &&
                            u.ParentId == parentId && u.TenantId == tenantId);
        }

        var resellers = await query
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

    [HttpPost("create-subreseller")]
    [Authorize(Policy = "ResellerOrAbove")]
    [RateLimit("user-create")]
    public async Task<IActionResult> CreateSubReseller([FromBody] CreateResellerRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var isSubReseller = request.IsSubReseller;
        if (actorRole == "Reseller" || actorRole == "SubReseller")
        {
            isSubReseller = true;
        }

        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.TenantId == tenantId))
            return BadRequest("El usuario ya existe.");

        var actor = await _context.Users.FindAsync(actorUserId);
        if (actor == null) return NotFound("Usuario no encontrado.");

        var creditsBefore = actor.Credits;

        var plans = request.PlanIds != null && request.PlanIds.Count > 0
            ? await _context.Plans.Where(p => request.PlanIds.Contains(p.Id)).ToListAsync()
            : new List<Plan>();
        decimal totalCost = plans.Sum(p => p.CreditCost);

        if (actorRole != "Admin" && actorRole != "SuperAdmin")
        {
            decimal totalRequired = request.InitialCredits;
            if (actor.Credits < totalRequired)
                return BadRequest("Creditos insuficientes para crear este reseller.");
        }

        var planNames = plans.Select(p => p.Name).ToList();

        var reseller = new User {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = request.UserName,
            PasswordHash = BC.HashPassword(request.Password),
            Role = isSubReseller ? UserRole.SubReseller : UserRole.Reseller,
            ParentId = actorUserId,
            Credits = request.InitialCredits,
            IsActive = true
        };

        _context.Users.Add(reseller);

        if (request.NodeIds != null && request.NodeIds.Count > 0)
        {
            foreach (var nodeId in request.NodeIds)
            {
                var node = await _context.VpsNodes.FindAsync(nodeId);
                if (node != null && node.TenantId == tenantId)
                {
                    _context.NodeAccesses.Add(new NodeAccess { Id = Guid.NewGuid(), UserId = reseller.Id, NodeId = nodeId });
                }
            }
        }

        if (plans.Count > 0)
        {
            foreach (var plan in plans)
            {
                _context.PlanAccesses.Add(new PlanAccess { Id = Guid.NewGuid(), UserId = reseller.Id, PlanId = plan.Id });
            }
        }

        if (actorRole != "Admin" && actorRole != "SuperAdmin" && request.InitialCredits > 0)
        {
            decimal totalDeduction = request.InitialCredits;
            actor.Credits -= totalDeduction;
            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SourceUserId = actorUserId,
                SourceBalanceBefore = creditsBefore,
                SourceBalanceAfter = actor.Credits,
                TargetUserId = reseller.Id,
                TargetBalanceBefore = request.InitialCredits,
                TargetBalanceAfter = request.InitialCredits,
                Amount = totalDeduction,
                Type = CreditTransactionType.UserCreation,
                CreatedAt = DateTime.UtcNow,
                Note = $"Creditos iniciales para sub-reseller {reseller.UserName}"
            });
        }

        await _context.SaveChangesAsync();

        var creditsAfter = actor.Credits;

        if (isSubReseller)
        {
            await _activityLogService.LogSubResellerCreatedAsync(
                actorUserId, reseller.Id, request.UserName,
                tenantId, actorRole, actorUserName,
                creditsBefore, creditsAfter, request.InitialCredits, planNames);
        }
        else
        {
            await _activityLogService.LogResellerCreatedAsync(
                actorUserId, reseller.Id, request.UserName,
                tenantId, actorRole, actorUserName,
                creditsBefore, creditsAfter, request.InitialCredits, planNames);
        }

        await _auditService.LogAsync("ResellerCreated",
            $"Reseller creado: {request.UserName} por {User.Identity?.Name}",
            actorUserId, tenantId);

        return Ok(new { Message = "Reseller creado", UserId = reseller.Id });
    }

    [HttpGet("clipboard-data/{id}")]
    [Authorize]
    public async Task<IActionResult> GetClipboardData(Guid id)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);

        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (actorRole != "Admin" && actorRole != "SuperAdmin" && user.ParentId != actorUserId)
            return Forbid();

        var nodeAccess = await _context.NodeAccesses
            .Include(na => na.Node)
            .Where(na => na.UserId == id)
            .FirstOrDefaultAsync();

        var nodeLabel = nodeAccess?.Node?.label;

        if (user.Role == UserRole.Customer)
        {
            return Ok(new
            {
                Type = "customer",
                UserName = user.UserName,
                Password = "[La contraseña no es accesible por seguridad]",
                ServiceExpiry = user.ServiceExpiry?.ToString("dd/MM/yyyy HH:mm") ?? "Sin vencimiento",
                Node = nodeLabel ?? "Sin asignar",
                MaxDevices = user.Plan?.MaxDevices ?? 1,
                MaxConnections = (user.Plan?.MaxConnections ?? 0) + user.AdditionalConnections
            });
        }

        if (user.Role == UserRole.Reseller || user.Role == UserRole.SubReseller)
        {
            return Ok(new
            {
                Type = "reseller",
                UserName = user.UserName,
                Password = "[La contraseña no es accesible por seguridad]",
                Credits = user.Credits,
                PanelUrl = "[URL del panel]"
            });
        }

        if (user.Role == UserRole.Admin)
        {
            return Ok(new
            {
                Type = "admin",
                UserName = user.UserName,
                Password = "[La contraseña no es accesible por seguridad]",
                PanelUrl = "[URL del panel]"
            });
        }

        return BadRequest(new { Message = "Tipo de usuario no soportado" });
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserBasicRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

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
                ClaimsHelper.GetCurrentUserId(User), user.Id, user.UserName,
                tenantId, ClaimsHelper.GetCurrentRole(User), ClaimsHelper.GetCurrentUserName(User), string.Join(", ", changes));
        }

        return Ok(new { Message = "Usuario actualizado correctamente", Changes = changes });
    }

    [HttpPost("{id}/toggle-suspend")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ToggleSuspend(Guid id)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        await _activityLogService.LogUserSuspendedAsync(
            actorUserId, user.Id, user.UserName,
            tenantId, actorRole, actorUserName, !user.IsActive);

        return Ok(new { Message = user.IsActive ? "Usuario activado" : "Usuario suspendido", IsActive = user.IsActive });
    }

    [HttpPost("{id}/suspend")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> SuspendUser(Guid id)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        if (actorRole != "Admin" && actorRole != "SuperAdmin" && user.ParentId != actorUserId)
            return Forbid();

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        await _activityLogService.LogUserSuspendedAsync(
            actorUserId, user.Id, user.UserName,
            tenantId, actorRole, actorUserName, !user.IsActive);

        return Ok(new { Message = user.IsActive ? "Usuario activado" : "Usuario suspendido", IsActive = user.IsActive });
    }

    [HttpPost("{id}/add-connection")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> AddConnection(Guid id, [FromBody] AddConnectionRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        int connectionsToAdd = request.ConnectionsToAdd > 0 ? request.ConnectionsToAdd : 1;

        Console.WriteLine($"[DEBUG] AddConnection - Target: {id}, Actor: {actorUserId}, Amount: {connectionsToAdd}");

        var result = await _userOperationService.AddConnectionsAsync(
            id, actorUserId, actorRole, actorUserName, tenantId, connectionsToAdd);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new
        {
            Message = result.Message,
            NewMaxConnections = result.NewMaxConnections,
            RemainingCredits = result.RemainingCredits
        });
    }

    [HttpPost("{id}/renew-plan")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> RenewPlan(Guid id, [FromBody] RenewPlanRequest request)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        Console.WriteLine($"[DEBUG] RenewPlan - Target: {id}, Plan: {request.PlanId}, Days: {request.DurationHours}");

        var result = await _userOperationService.RenewPlanAsync(
            id, actorUserId, actorRole, actorUserName, tenantId, 
            request.PlanId, request.DurationHours);

        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new
        {
            Message = result.Message,
            PlanName = result.PlanName,
            ServiceExpiry = result.ServiceExpiry,
            RemainingCredits = result.RemainingCredits
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var actorUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var actorRole = ClaimsHelper.GetCurrentRole(User);
        var actorUserName = ClaimsHelper.GetCurrentUserName(User);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        var deletedUserName = user.UserName;

        // Remove child users and their related records
        var childUserIds = _context.Users.Where(u => u.ParentId == id).Select(u => u.Id).ToList();
        foreach (var childId in childUserIds)
        {
            _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == childId));
            _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == childId));
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs.Where(al => al.ActorUserId == childId || al.TargetUserId == childId));
            _context.Notifications.RemoveRange(_context.Notifications.Where(n => n.UserId == childId));
            _context.SessionLogs.RemoveRange(_context.SessionLogs.Where(sl => sl.UserId == childId));
        }
        _context.Users.RemoveRange(_context.Users.Where(u => u.ParentId == id));

        // Remove user related records
        _context.NodeAccesses.RemoveRange(_context.NodeAccesses.Where(na => na.UserId == id));
        _context.PlanAccesses.RemoveRange(_context.PlanAccesses.Where(pa => pa.UserId == id));
        _context.ActivityLogs.RemoveRange(_context.ActivityLogs.Where(al => al.ActorUserId == id || al.TargetUserId == id));
        _context.Notifications.RemoveRange(_context.Notifications.Where(n => n.UserId == id));
        _context.SessionLogs.RemoveRange(_context.SessionLogs.Where(sl => sl.UserId == id));

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await _activityLogService.LogUserDeletedAsync(
            actorUserId, user.Id, deletedUserName,
            tenantId, actorRole, actorUserName);

        return Ok(new { Message = "Usuario eliminado" });
    }

    [HttpPost("{id}/nodes")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateUserNodes(Guid id, [FromBody] UpdateUserNodesRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenantId);
        if (user == null) return NotFound(new { Message = "Usuario no encontrado." });

        var existing = _context.NodeAccesses.Where(na => na.UserId == id);
        _context.NodeAccesses.RemoveRange(existing);

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
                await GetSubordinateChildrenAsync(s.Id)
            ));
        }
        return nodes;
    }

    #endregion
}
