using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;

namespace DevNetControl.Api.Infrastructure.Services;

public class CreditService
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;

    public CreditService(ApplicationDbContext context, NotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<decimal> GetBalanceAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Credits ?? 0;
    }

    public async Task<(bool Success, string Message)> TransferCreditsAsync(Guid sourceUserId, Guid targetUserId, decimal amount)
    {
        if (amount <= 0) return (false, "Monto inválido.");
        var source = await _context.Users.FindAsync(sourceUserId);
        var target = await _context.Users.FindAsync(targetUserId);

        if (source == null || target == null) return (false, "Usuario no encontrado.");
        if (source.Role != UserRole.Admin && source.Credits < amount) return (false, "Saldo insuficiente.");

        var sourceBalanceBefore = source.Credits;
        var targetBalanceBefore = target.Credits;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (source.Role != UserRole.Admin) source.Credits -= amount;
            target.Credits += amount;

            _context.CreditTransactions.Add(new CreditTransaction
            {
                Id = Guid.NewGuid(),
                TenantId = source.TenantId,
                SourceUserId = sourceUserId,
                SourceBalanceBefore = sourceBalanceBefore,
                SourceBalanceAfter = source.Credits,
                TargetUserId = targetUserId,
                TargetBalanceBefore = targetBalanceBefore,
                TargetBalanceAfter = target.Credits,
                Amount = amount,
                Type = CreditTransactionType.Transfer,
                CreatedAt = DateTime.UtcNow
            });

            _context.ActivityLogs.Add(new ActivityLog
            {
                ActionType = ActivityActionType.CreditsTransferred,
                ActorUserId = sourceUserId,
                ActorUserName = source.UserName,
                ActorRole = source.Role.ToString(),
                TargetUserId = targetUserId,
                TargetUserName = target.UserName,
                CreditsConsumed = amount,
                CreditsBalanceBefore = sourceBalanceBefore,
                CreditsBalanceAfter = source.Credits,
                Description = $"Transferencia de {amount} créditos a '{target.UserName}'. Saldo origen: {sourceBalanceBefore} -> {source.Credits}. Saldo destino: {targetBalanceBefore} -> {target.Credits}",
                TenantId = source.TenantId
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (source.Role != UserRole.Admin && source.Credits <= 10m)
                await _notificationService.GenerateLowCreditAlertsAsync();

            return (true, "Transferencia exitosa.");
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, "Error interno.");
        }
    }

    public async Task<(bool Success, string Message)> AddCreditsAsync(Guid targetUserId, decimal amount, Guid tenantId, Guid? actorUserId = null, string? actorRole = null, string? actorUserName = null)
    {
        var targetUser = await _context.Users.FindAsync(targetUserId);
        if (targetUser == null) return (false, "Usuario no encontrado.");

        var targetBalanceBefore = targetUser.Credits;

        targetUser.Credits += amount;

        _context.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceUserId = targetUserId,
            SourceBalanceBefore = targetBalanceBefore,
            SourceBalanceAfter = targetUser.Credits,
            TargetUserId = targetUserId,
            TargetBalanceBefore = targetBalanceBefore,
            TargetBalanceAfter = targetUser.Credits,
            Amount = amount,
            Type = CreditTransactionType.AdminCredit,
            CreatedAt = DateTime.UtcNow
        });

        if (actorUserId.HasValue && !string.IsNullOrEmpty(actorUserName))
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                ActionType = ActivityActionType.CreditsLoaded,
                ActorUserId = actorUserId.Value,
                ActorUserName = actorUserName,
                ActorRole = actorRole ?? "Admin",
                TargetUserId = targetUserId,
                TargetUserName = targetUser.UserName,
                CreditsConsumed = 0,
                CreditsBalanceBefore = targetBalanceBefore,
                CreditsBalanceAfter = targetUser.Credits,
                Description = $"Carga de {amount} créditos a '{targetUser.UserName}'. Saldo: {targetBalanceBefore} -> {targetUser.Credits}",
                TenantId = tenantId
            });
        }

        await _context.SaveChangesAsync();

        if (targetUser.Credits <= 10m)
            await _notificationService.GenerateLowCreditAlertsAsync();

        return (true, "Créditos agregados correctamente.");
    }
}
