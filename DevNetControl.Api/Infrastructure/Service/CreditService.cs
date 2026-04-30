using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

public class CreditService
{
    private readonly ApplicationDbContext _context;

    public CreditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> TransferCreditsAsync(Guid fromId, Guid toId, decimal amount, Guid tenantId)
    {
        if (amount <= 0) return (false, "El monto debe ser mayor a cero.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sender = await _context.Users.FindAsync(fromId);
            var receiver = await _context.Users.FindAsync(toId);

            if (sender == null || receiver == null)
                return (false, "Usuario no encontrado.");

            if (sender.TenantId != tenantId || receiver.TenantId != tenantId)
                return (false, "No puedes transferir fuera de tu organizacion.");

            if (sender.Credits < amount)
                return (false, "Saldo insuficiente.");

            if (sender.Role != UserRole.Admin && sender.Role != UserRole.SuperAdmin && receiver.ParentId != sender.Id)
                return (false, "No tienes permiso para transferir a este usuario.");

            sender.Credits -= amount;
            receiver.Credits += amount;

            var audit = new CreditTransaction
            {
                TenantId = tenantId,
                FromUserId = fromId,
                ToUserId = toId,
                Amount = amount,
                Note = $"Transferencia de {sender.UserName} a {receiver.UserName}"
            };

            _context.CreditTransactions.Add(audit);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return (true, "Transferencia exitosa.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error interno: {ex.Message}");
        }
    }

    public async Task<decimal> GetUserBalanceAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Credits ?? 0;
    }

    public async Task<List<TransactionDto>> GetTransactionHistoryAsync(Guid userId, Guid tenantId, int limit = 50)
    {
        var transactions = await _context.CreditTransactions
            .Where(t => (t.FromUserId == userId || t.ToUserId == userId) && t.TenantId == tenantId)
            .OrderByDescending(t => t.Timestamp)
            .Take(limit)
            .Include(t => t.FromUser)
            .Include(t => t.ToUser)
            .Select(t => new TransactionDto(
                t.Id,
                t.Timestamp,
                t.FromUserId,
                t.ToUserId,
                t.FromUser.UserName,
                t.ToUser.UserName,
                t.Amount,
                t.FromUserId == userId ? "Sent" : "Received",
                t.Note
            ))
            .ToListAsync();

        return transactions;
    }

    public async Task<(bool Success, string Message)> AddCreditsAsync(Guid userId, decimal amount, Guid tenantId)
    {
        if (amount <= 0) return (false, "El monto debe ser mayor a cero.");

        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return (false, "Usuario no encontrado.");

            if (user.TenantId != tenantId)
                return (false, "El usuario no pertenece a tu organizacion.");

            user.Credits += amount;

            var audit = new CreditTransaction
            {
                TenantId = tenantId,
                FromUserId = userId,
                ToUserId = userId,
                Amount = amount,
                Note = $"Creditos agregados manualmente: +{amount}"
            };

            _context.CreditTransactions.Add(audit);
            await _context.SaveChangesAsync();

            return (true, $"Se agregaron {amount} creditos a {user.UserName}.");
        }
        catch (Exception ex)
        {
            return (false, $"Error interno: {ex.Message}");
        }
    }
}

public record TransactionDto(
    Guid Id,
    DateTime Timestamp,
    Guid FromUserId,
    Guid ToUserId,
    string FromUserName,
    string ToUserName,
    decimal Amount,
    string Direction,
    string Note
);
