using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;

namespace DevNetControl.Api.Infrastructure.Services;

public class CreditService
{
    private readonly ApplicationDbContext _context;

    public CreditService(ApplicationDbContext context) => _context = context;

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

        using var transaction = await _context.Database.BeginTransactionAsync();
        try {
            if (source.Role != UserRole.Admin) source.Credits -= amount;
            target.Credits += amount;

            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = source.TenantId,
                SourceUserId = sourceUserId,
                TargetUserId = targetUserId,
                Amount = amount,
                Type = CreditTransactionType.Transfer,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, "Transferencia exitosa.");
        } catch {
            await transaction.RollbackAsync();
            return (false, "Error interno.");
        }
    }

    // Firma corregida para aceptar 3 argumentos (targetUserId, amount, tenantId)
    public async Task<(bool Success, string Message)> AddCreditsAsync(Guid targetUserId, decimal amount, Guid tenantId)
    {
        var targetUser = await _context.Users.FindAsync(targetUserId);
        if (targetUser == null) return (false, "Usuario no encontrado.");

        targetUser.Credits += amount;
        
        _context.CreditTransactions.Add(new CreditTransaction {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceUserId = targetUserId, 
            TargetUserId = targetUserId,
            Amount = amount,
            Type = CreditTransactionType.AdminCredit, 
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, "Créditos agregados correctamente.");
    }
}