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

    public async Task<(bool Success, string Message)> TransferCreditsAsync(Guid fromId, Guid toId, decimal amount)
    {
        if (amount <= 0) return (false, "El monto debe ser mayor a cero.");

        // Usamos una transacción de DB para que sea todo o nada
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var sender = await _context.Users.FindAsync(fromId);
            var receiver = await _context.Users.FindAsync(toId);

            if (sender == null || receiver == null)
                return (false, "Usuario no encontrado.");

            if (sender.Credits < amount)
                return (false, "Saldo insuficiente.");

            // Lógica de jerarquía (opcional pero recomendada): 
            // Solo puedes transferir a alguien que sea tu "hijo" o si eres Admin
            if (sender.Role != UserRole.Admin && receiver.ParentId != sender.Id)
                return (false, "No tienes permiso para transferir a este usuario.");

            // Realizar movimiento
            sender.Credits -= amount;
            receiver.Credits += amount;

            // Registrar la auditoría
            var audit = new CreditTransaction
            {
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
}