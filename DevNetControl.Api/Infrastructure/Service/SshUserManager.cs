using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

public class SshUserManager
{
    private readonly ApplicationDbContext _context;
    private readonly SshService _sshService;

    public SshUserManager(ApplicationDbContext context, SshService sshService)
    {
        _context = context;
        _sshService = sshService;
    }

    public async Task<(bool Success, string Message)> ProvisionUserAsync(Guid nodeId, Guid userId, string plainPassword, Guid adminId)
    {
        var user = await _context.Users.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Plan == null) return (false, "Datos incompletos.");

        var actualCost = user.Plan.CreditCost * user.Plan.MaxConnections;
        if (user.Credits < actualCost && user.Role != UserRole.Admin) return (false, "Créditos insuficientes.");

        var setupCmd = $"useradd -m {user.UserName} && echo '{user.UserName}:{plainPassword}' | chpasswd";
        
        // CORRECCIÓN: SshService devuelve una tupla (Success, Output, Error)
        var result = await _sshService.ExecuteCommandAsync(nodeId, setupCmd);
        
        if (result.Success) {
            if (user.Role != UserRole.Admin) user.Credits -= actualCost;
            user.IsProvisionedOnVps = true;
            _context.CreditTransactions.Add(new CreditTransaction {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                SourceUserId = adminId,
                TargetUserId = userId,
                Amount = actualCost,
                Type = CreditTransactionType.UserCreation,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return (true, "Usuario provisionado con éxito.");
        }
        
        return (false, $"Error de SSH: {result.Error}");
    }

    // MÉTODO REQUERIDO POR BACKGROUND SERVICES (Error CS1061)
    public async Task<(bool Success, string Output, string Error)> DeleteUserFromVpsAsync(Guid nodeId, string username)
    {
        var cmd = $"userdel -r {username}";
        return await _sshService.ExecuteCommandAsync(nodeId, cmd);
    }

    // MÉTODOS DE COMPATIBILIDAD
    public async Task<(bool Success, string Output, string Error)> CreateUserOnVpsAsync(Guid nodeId, string username, string password, int maxDevices, DateTime? expiry)
    {
        return await _sshService.ExecuteCommandAsync(nodeId, $"useradd -m {username}");
    }

    public async Task<(bool Success, string Output, string Error)> ExtendUserExpiryOnVpsAsync(Guid nodeId, string username, DateTime expiry)
    {
        return await _sshService.ExecuteCommandAsync(nodeId, $"chage -E {expiry:yyyy-MM-dd} {username}");
    }
}