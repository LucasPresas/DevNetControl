using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

public class UserProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly SshUserManager _sshUserManager;

    public UserProvisioningService(ApplicationDbContext context, SshUserManager sshUserManager)
    {
        _context = context;
        _sshUserManager = sshUserManager;
    }

    public async Task<(bool Success, string Message)> ProvisionNewUserAsync(Guid userId, Guid nodeId, Guid adminId)
    {
        // ... lógica de búsqueda de usuario y nodo ...
        
        // Simulación de la creación de la transacción de auditoría corregida
        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.Empty, // Reemplazar con el tenant real
            SourceUserId = adminId, // Antes FromUserId
            TargetUserId = userId,  // Antes ToUserId
            Amount = 0, // Definir costo real
            Type = CreditTransactionType.UserCreation, // Antes UserCreationCost
            CreatedAt = DateTime.UtcNow
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        
        return (true, "Proceso completado.");
    }
}