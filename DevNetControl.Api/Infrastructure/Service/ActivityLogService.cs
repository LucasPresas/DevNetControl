using System.Text.Json;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

public class ActivityLogService
{
    private readonly ApplicationDbContext _context;

    public ActivityLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogUserCreatedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal creditsBefore, decimal creditsAfter, Guid? planId = null, string? planName = null)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.UserCreated,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = creditsBefore - creditsAfter,
            CreditsBalanceBefore = creditsBefore,
            CreditsBalanceAfter = creditsAfter,
            PlanId = planId,
            PlanName = planName,
            TenantId = tenantId,
            Description = $"**{actorUserName}** creó el usuario '{targetUserName}' con el plan '{planName}'. Balance: {creditsAfter}"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogResellerCreatedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal creditsBefore, decimal creditsAfter, decimal initialCreditsAssigned,
        List<string> planNames)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.ResellerCreated,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = creditsBefore - creditsAfter,
            CreditsBalanceBefore = creditsBefore,
            CreditsBalanceAfter = creditsAfter,
            Description = $"Reseller '{targetUserName}' creado. Créditos asignados: {initialCreditsAssigned}. Planes: {string.Join(", ", planNames)}",
            TenantId = tenantId,
            Details = JsonSerializer.Serialize(new
            {
                InitialCredits = initialCreditsAssigned,
                Plans = planNames,
                CreditsRemaining = creditsAfter
            })
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogSubResellerCreatedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal creditsBefore, decimal creditsAfter, decimal initialCreditsAssigned,
        List<string> planNames)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.SubResellerCreated,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = creditsBefore - creditsAfter,
            CreditsBalanceBefore = creditsBefore,
            CreditsBalanceAfter = creditsAfter,
            Description = $"SubReseller '{targetUserName}' creado. Créditos asignados: {initialCreditsAssigned}. Planes: {string.Join(", ", planNames)}",
            TenantId = tenantId,
            Details = JsonSerializer.Serialize(new
            {
                InitialCredits = initialCreditsAssigned,
                Plans = planNames,
                CreditsRemaining = creditsAfter
            })
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogCreditsTransferredAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal amount, decimal sourceCreditsBefore, decimal sourceCreditsAfter,
        decimal targetCreditsBefore, decimal targetCreditsAfter)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.CreditsTransferred,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = amount,
            CreditsBalanceBefore = sourceCreditsBefore,
            CreditsBalanceAfter = sourceCreditsAfter,
            Description = $"Transferencia de {amount} créditos a '{targetUserName}'. Saldo origen: {sourceCreditsBefore} -> {sourceCreditsAfter}. Saldo destino: {targetCreditsBefore} -> {targetCreditsAfter}",
            TenantId = tenantId,
            Details = JsonSerializer.Serialize(new
            {
                Amount = amount,
                SourceBalanceBefore = sourceCreditsBefore,
                SourceBalanceAfter = sourceCreditsAfter,
                TargetBalanceBefore = targetCreditsBefore,
                TargetBalanceAfter = targetCreditsAfter
            })
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogCreditsLoadedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal amount, decimal targetCreditsBefore, decimal targetCreditsAfter)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.CreditsLoaded,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            CreditsBalanceBefore = targetCreditsBefore,
            CreditsBalanceAfter = targetCreditsAfter,
            Description = $"Carga de {amount} créditos a '{targetUserName}'. Saldo: {targetCreditsBefore} -> {targetCreditsAfter}",
            TenantId = tenantId
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogPlanAssignedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        Guid planId, string planName, decimal creditCost,
        decimal creditsBefore, decimal creditsAfter)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.PlanAssigned,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = creditCost,
            CreditsBalanceBefore = creditsBefore,
            CreditsBalanceAfter = creditsAfter,
            PlanId = planId,
            PlanName = planName,
            TenantId = tenantId,
            Description = $"Plan '{planName}' asignado a '{targetUserName}'. Costo: {creditCost} créditos. Saldo restante: {creditsAfter}"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogServiceExtendedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName, int days)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.ServiceExtended,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            TenantId = tenantId,
            Description = $"Servicio extendido {days} días para '{targetUserName}'"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserDeletedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.UserDeleted,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            TenantId = tenantId,
            Description = $"Usuario '{targetUserName}' eliminado"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserSuspendedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName, bool isSuspended)
    {
        var action = isSuspended ? ActivityActionType.UserSuspended : ActivityActionType.UserUpdated;
        var log = new ActivityLog
        {
            ActionType = action,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            TenantId = tenantId,
            Description = isSuspended
                ? $"Usuario '{targetUserName}' suspendido"
                : $"Usuario '{targetUserName}' reactivado"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserUpdatedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName, string changesDescription)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.UserUpdated,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            TenantId = tenantId,
            Description = $"Usuario '{targetUserName}' actualizado: {changesDescription}"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogNodeAccessGrantedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        Guid nodeId, string nodeLabel)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.NodeAccessGranted,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            NodeId = nodeId,
            NodeLabel = nodeLabel,
            TenantId = tenantId,
            Description = $"Acceso al nodo '{nodeLabel}' otorgado a '{targetUserName}'"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogPlanAccessGrantedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        Guid planId, string planName)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.PlanAccessGranted,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = 0,
            PlanId = planId,
            PlanName = planName,
            TenantId = tenantId,
            Description = $"Acceso al plan '{planName}' otorgado a '{targetUserName}'"
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogBulkOperationAsync(
        Guid actorUserId, Guid tenantId, string actorRole, string actorUserName,
        string operationType, int totalProcessed, int successCount, int failCount)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.BulkOperation,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            CreditsConsumed = 0,
            TenantId = tenantId,
            Description = $"Operación masiva '{operationType}': {successCount} exitosos, {failCount} fallidos de {totalProcessed} totales",
            Details = JsonSerializer.Serialize(new
            {
                OperationType = operationType,
                TotalProcessed = totalProcessed,
                SuccessCount = successCount,
                FailCount = failCount
            })
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogCreditsConsumedAsync(
        Guid actorUserId, Guid targetUserId, string targetUserName,
        Guid tenantId, string actorRole, string actorUserName,
        decimal creditsConsumed, decimal creditsBefore, decimal creditsAfter,
        string description)
    {
        var log = new ActivityLog
        {
            ActionType = ActivityActionType.CreditsConsumed,
            ActorUserId = actorUserId,
            ActorUserName = actorUserName,
            ActorRole = actorRole,
            TargetUserId = targetUserId,
            TargetUserName = targetUserName,
            CreditsConsumed = creditsConsumed,
            CreditsBalanceBefore = creditsBefore,
            CreditsBalanceAfter = creditsAfter,
            TenantId = tenantId,
            Description = description
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
