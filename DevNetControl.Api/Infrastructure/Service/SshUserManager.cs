using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace DevNetControl.Api.Infrastructure.Services;

public class SshUserManager
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;
    private readonly SshService _sshService;

    public SshUserManager(ApplicationDbContext context, EncryptionService encryption, SshService sshService)
    {
        _context = context;
        _encryption = encryption;
        _sshService = sshService;
    }

    // REFACTOR: Provisión con validación de lógica de negocio (Créditos x Dispositivos)
    public async Task<(bool Success, string Message)> ProvisionUserAsync(Guid nodeId, Guid userId, string plainPassword)
    {
        var user = await _context.Users.Include(u => u.Plan).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Plan == null) return (false, "Datos incompletos.");

        // Lógica de costo proporcional: 1 dispositivo = 1 crédito base del plan
        var actualCost = user.Plan.CreditCost * user.Plan.MaxConnections;

        if (user.Credits < actualCost && user.Role != UserRole.Admin)
            return (false, "Créditos insuficientes para este nivel de dispositivos.");

        // Ejecución en el VPS[cite: 1]
        var expiryStr = user.ServiceExpiry?.ToString("yyyy-MM-dd");
        
        // Comando maestro: Crea usuario, setea pass, setea expiración y limita MaxSessions nativo
        var setupCmd = $@"
            useradd -m -s /bin/bash {user.UserName} && 
            echo '{user.UserName}:{plainPassword}' | chpasswd && 
            chage -E {expiryStr ?? "never"} {user.UserName} &&
            mkdir -p /etc/ssh/sshd_config.d &&
            echo 'Match User {user.UserName}
                MaxSessions {user.Plan.MaxConnections}
                MaxStartups {user.Plan.MaxConnections}' > /etc/ssh/sshd_config.d/{user.UserName}.conf &&
            systemctl reload ssh 2>/dev/null || service ssh reload
        ";

        var result = await _sshService.ExecuteCommandAsync(nodeId, setupCmd);
        
        if (result.Success)
        {
            user.Credits -= actualCost; // Debitar créditos[cite: 1]
            user.IsProvisionedOnVps = true;
            await _context.SaveChangesAsync();
        }

        return (result.Success, result.Success ? "Usuario provisionado" : result.Error);
    }


        // Error CS1061: CreateUserOnVpsAsync
    public async Task<(bool Success, string Output, string Error)> CreateUserOnVpsAsync(Guid nodeId, string username, string password, int maxDevices, DateTime? expiryDate)
    {
        // Reutiliza la lógica del nuevo método ProvisionUserAsync
        var result = await ProvisionUserAsync(nodeId, Guid.Empty, password); // Guid.Empty temporal para compatibilidad
        return (result.Success, result.Message, result.Success ? "" : result.Message);
    }

    // Error CS1061: DeleteUserFromVpsAsync
    public async Task<(bool Success, string Output, string Error)> DeleteUserFromVpsAsync(Guid nodeId, string username)
    {
        var cmd = $"userdel -r {username} && rm -f /etc/ssh/sshd_config.d/{username}.conf && systemctl reload ssh";
        var result = await _sshService.ExecuteCommandAsync(nodeId, cmd);
        return (result.Success, result.Output, result.Error);
    }

    // Error CS1061: ExtendUserExpiryOnVpsAsync
    public async Task<(bool Success, string Output, string Error)> ExtendUserExpiryOnVpsAsync(Guid nodeId, string username, DateTime newExpiryDate)
    {
        var expiryStr = newExpiryDate.ToString("yyyy-MM-dd");
        var cmd = $"chage -E {expiryStr} {username}";
        var result = await _sshService.ExecuteCommandAsync(nodeId, cmd);
        return (result.Success, result.Output, result.Error);
    }
}