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
public class ActivityController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ActivityController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetActivities(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] ActivityActionType? actionType = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] Guid? targetUserId = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { Message = "Parámetros de paginación inválidos." });

        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var currentRole = ClaimsHelper.GetCurrentRole(User);

        var query = _context.ActivityLogs
            .Include(al => al.ActorUser)
            .Include(al => al.TargetUser)
            .Where(al => al.TenantId == tenantId)
            .AsQueryable();

        var roleValue = ParseRole(currentRole);
        if (roleValue > UserRole.Admin)
        {
            var visibleActorIds = await GetVisibleActorIdsAsync(currentUserId, tenantId, roleValue);
            query = query.Where(al => visibleActorIds.Contains(al.ActorUserId));
        }

        if (actionType.HasValue)
            query = query.Where(al => al.ActionType == actionType.Value);

        if (actorUserId.HasValue)
            query = query.Where(al => al.ActorUserId == actorUserId.Value);

        if (targetUserId.HasValue)
            query = query.Where(al => al.TargetUserId == targetUserId.Value);

        if (dateFrom.HasValue)
            query = query.Where(al => al.Timestamp >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(al => al.Timestamp <= dateTo.Value);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(al =>
                al.ActorUserName.Contains(search) ||
                (al.TargetUserName != null && al.TargetUserName.Contains(search)) ||
                al.Description.Contains(search) ||
                al.ActionType.ToString().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var activities = await query
            .OrderByDescending(al => al.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => new ActivityLogDto(
                al.Id,
                al.ActionType.ToString(),
                al.ActorUserId,
                al.ActorUserName,
                al.ActorRole,
                al.TargetUserId,
                al.TargetUserName,
                al.CreditsConsumed,
                al.CreditsBalanceBefore,
                al.CreditsBalanceAfter,
                al.Description,
                al.Details,
                al.PlanId,
                al.PlanName,
                al.NodeId,
                al.NodeLabel,
                al.Timestamp
            ))
            .ToListAsync();

        return Ok(new
        {
            Total = totalCount,
            Page = page,
            PageSize = pageSize,
            Data = activities
        });
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentActivities([FromQuery] int limit = 10)
    {
        if (limit < 1 || limit > 50)
            limit = 10;

        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var currentRole = ClaimsHelper.GetCurrentRole(User);

        var query = _context.ActivityLogs
            .Where(al => al.TenantId == tenantId)
            .AsQueryable();

        var roleValue = ParseRole(currentRole);
        if (roleValue > UserRole.Admin)
        {
            var visibleActorIds = await GetVisibleActorIdsAsync(currentUserId, tenantId, roleValue);
            query = query.Where(al => visibleActorIds.Contains(al.ActorUserId));
        }

        var activities = await query
            .OrderByDescending(al => al.Timestamp)
            .Take(limit)
            .Select(al => new ActivityLogDto(
                al.Id,
                al.ActionType.ToString(),
                al.ActorUserId,
                al.ActorUserName,
                al.ActorRole,
                al.TargetUserId,
                al.TargetUserName,
                al.CreditsConsumed,
                al.CreditsBalanceBefore,
                al.CreditsBalanceAfter,
                al.Description,
                al.Details,
                al.PlanId,
                al.PlanName,
                al.NodeId,
                al.NodeLabel,
                al.Timestamp
            ))
            .ToListAsync();

        return Ok(activities);
    }

    private static UserRole ParseRole(string role) => role switch
    {
        "SuperAdmin" => UserRole.SuperAdmin,
        "Admin" => UserRole.Admin,
        "Reseller" => UserRole.Reseller,
        "SubReseller" => UserRole.SubReseller,
        _ => UserRole.Customer
    };

    private async Task<HashSet<Guid>> GetVisibleActorIdsAsync(Guid currentUserId, Guid tenantId, UserRole currentRole)
    {
        var ids = new HashSet<Guid> { currentUserId };
        await CollectSubordinateIdsAsync(currentUserId, tenantId, currentRole, ids);
        return ids;
    }

    private async Task CollectSubordinateIdsAsync(Guid parentId, Guid tenantId, UserRole parentRole, HashSet<Guid> ids)
    {
        var subordinates = await _context.Users
            .Where(u => u.ParentId == parentId && u.TenantId == tenantId && u.Role > parentRole)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var subId in subordinates)
        {
            if (ids.Add(subId))
            {
                await CollectSubordinateIdsAsync(subId, tenantId, parentRole, ids);
            }
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetActivityDetail(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var activity = await _context.ActivityLogs
            .Include(al => al.ActorUser)
            .Include(al => al.TargetUser)
            .FirstOrDefaultAsync(al => al.Id == id && al.TenantId == tenantId);

        if (activity == null)
            return NotFound(new { Message = "Registro de actividad no encontrado." });

        var creditTransaction = await _context.CreditTransactions
            .Where(ct => ct.TenantId == tenantId &&
                        (ct.SourceUserId == activity.ActorUserId || ct.TargetUserId == activity.TargetUserId) &&
                        Math.Abs(ct.Amount - activity.CreditsConsumed) < 0.01m &&
                        Math.Abs((ct.CreatedAt - activity.Timestamp).TotalSeconds) < 5)
            .OrderByDescending(ct => ct.CreatedAt)
            .FirstOrDefaultAsync();

        return Ok(new ActivityLogDetailDto(
            activity.Id,
            activity.ActionType.ToString(),
            activity.ActorUserId,
            activity.ActorUserName,
            activity.ActorRole,
            activity.TargetUserId,
            activity.TargetUserName,
            activity.CreditsConsumed,
            activity.CreditsBalanceBefore,
            activity.CreditsBalanceAfter,
            activity.Description,
            activity.Details,
            activity.PlanId,
            activity.PlanName,
            activity.NodeId,
            activity.NodeLabel,
            activity.Timestamp,
            creditTransaction?.SourceBalanceBefore,
            creditTransaction?.SourceBalanceAfter,
            creditTransaction?.TargetBalanceBefore,
            creditTransaction?.TargetBalanceAfter
        ));
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetActivitiesByUser(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { Message = "Parámetros de paginación inválidos." });

        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var currentRole = ClaimsHelper.GetCurrentRole(User);

        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);

        if (targetUser == null)
            return NotFound(new { Message = "Usuario no encontrado." });

        if (currentUserId != userId && currentRole != "Admin" && currentRole != "SuperAdmin")
        {
            var isSuperior = await IsUserSuperiorAsync(currentUserId, userId, tenantId);
            if (!isSuperior)
                return Forbid();
        }

        var query = _context.ActivityLogs
            .Where(al => al.TenantId == tenantId &&
                        (al.ActorUserId == userId || al.TargetUserId == userId))
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var activities = await query
            .OrderByDescending(al => al.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => new ActivityLogDto(
                al.Id,
                al.ActionType.ToString(),
                al.ActorUserId,
                al.ActorUserName,
                al.ActorRole,
                al.TargetUserId,
                al.TargetUserName,
                al.CreditsConsumed,
                al.CreditsBalanceBefore,
                al.CreditsBalanceAfter,
                al.Description,
                al.Details,
                al.PlanId,
                al.PlanName,
                al.NodeId,
                al.NodeLabel,
                al.Timestamp
            ))
            .ToListAsync();

        return Ok(new
        {
            Total = totalCount,
            Page = page,
            PageSize = pageSize,
            UserName = targetUser.UserName,
            UserRole = targetUser.Role.ToString(),
            Data = activities
        });
    }

    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetStats()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var today = DateTime.UtcNow.Date;

        var totalActivities = await _context.ActivityLogs
            .CountAsync(al => al.TenantId == tenantId);

        var uniqueActors = await _context.ActivityLogs
            .Where(al => al.TenantId == tenantId)
            .Select(al => al.ActorUserId)
            .Distinct()
            .CountAsync();

        var activitiesLast24Hours = await _context.ActivityLogs
            .CountAsync(al => al.TenantId == tenantId &&
                             al.Timestamp >= DateTime.UtcNow.AddHours(-24));

        var activitiesToday = await _context.ActivityLogs
            .CountAsync(al => al.TenantId == tenantId && al.Timestamp >= today);

        var topActionsRaw = await _context.ActivityLogs
            .Where(al => al.TenantId == tenantId)
            .GroupBy(al => al.ActionType)
            .Select(g => new { ActionType = g.Key, Count = g.Count() })
            .ToListAsync();

        var topActions = topActionsRaw
            .Select(x => new ActionCountDto(x.ActionType.ToString(), x.Count))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var activitiesByRoleRaw = await _context.ActivityLogs
            .Where(al => al.TenantId == tenantId)
            .GroupBy(al => al.ActorRole)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        var activitiesByRole = activitiesByRoleRaw
            .Select(x => new RoleCountDto(x.Role, x.Count))
            .OrderByDescending(x => x.Count)
            .ToList();

        var totalCreditsConsumed = await _context.ActivityLogs
            .Where(al => al.TenantId == tenantId)
            .SumAsync(al => al.CreditsConsumed);

        return Ok(new ActivityStatsDto(
            totalActivities,
            uniqueActors,
            activitiesLast24Hours,
            activitiesToday,
            topActions,
            activitiesByRole,
            totalCreditsConsumed,
            DateTime.UtcNow
        ));
    }

    [HttpGet("credits/summary")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetCreditsSummary(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var query = _context.CreditTransactions
            .Where(ct => ct.TenantId == tenantId)
            .AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(ct => ct.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(ct => ct.CreatedAt <= dateTo.Value);

        var transactions = await query
            .Include(ct => ct.SourceUser)
            .Include(ct => ct.TargetUser)
            .OrderByDescending(ct => ct.CreatedAt)
            .Select(ct => new
            {
                ct.Id,
                ct.Type,
                ct.Amount,
                ct.Note,
                ct.CreatedAt,
                SourceUserName = ct.SourceUser != null ? ct.SourceUser.UserName : "Sistema",
                TargetUserName = ct.TargetUser != null ? ct.TargetUser.UserName : "Sistema",
                ct.SourceBalanceBefore,
                ct.SourceBalanceAfter,
                ct.TargetBalanceBefore,
                ct.TargetBalanceAfter
            })
            .ToListAsync();

        var totalConsumed = await query
            .Where(ct => ct.Type != CreditTransactionType.AdminCredit && ct.Type != CreditTransactionType.Refund)
            .SumAsync(ct => ct.Amount);

        var totalAdded = await query
            .Where(ct => ct.Type == CreditTransactionType.AdminCredit)
            .SumAsync(ct => ct.Amount);

        return Ok(new
        {
            TotalConsumed = totalConsumed,
            TotalAdded = totalAdded,
            NetBalance = totalAdded - totalConsumed,
            TransactionCount = transactions.Count,
            Transactions = transactions
        });
    }

    private async Task<bool> IsUserSuperiorAsync(Guid currentUserId, Guid targetUserId, Guid tenantId)
    {
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == currentUserId && u.TenantId == tenantId);

        if (currentUser == null) return false;

        if (currentUser.Role == UserRole.Admin || currentUser.Role == UserRole.SuperAdmin)
            return true;

        return await IsInHierarchyAsync(currentUserId, targetUserId);
    }

    private async Task<bool> IsInHierarchyAsync(Guid potentialParentId, Guid targetUserId)
    {
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == targetUserId);

        if (targetUser == null) return false;

        if (targetUser.ParentId == potentialParentId) return true;

        if (targetUser.ParentId == null) return false;

        return await IsInHierarchyAsync(potentialParentId, targetUser.ParentId.Value);
    }
}
