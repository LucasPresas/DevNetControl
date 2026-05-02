using DevNetControl.Api.Domain;
using DevNetControl.Api.Dtos;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Infrastructure.Services;

public class UserProvisioningService
{
    private readonly ApplicationDbContext _context;
    private readonly SshUserManager _sshUserManager;
    private readonly CreditService _creditService;

    public UserProvisioningService(ApplicationDbContext context, SshUserManager sshUserManager, CreditService creditService)
    {
        _context = context;
        _sshUserManager = sshUserManager;
        _creditService = creditService;
    }

    public async Task<(bool Success, string Message, Guid? UserId)> CreateUserAsync(
        Guid parentId, Guid tenantId, string userName, string password, Guid? planId, Guid? nodeId)
    {
        if (!planId.HasValue)
            return (false, "PlanId es requerido.", null);

        if (await _context.Users.AnyAsync(u => u.UserName == userName && u.TenantId == tenantId))
            return (false, "El usuario ya existe.", null);

        var plan = await _context.Plans.FindAsync(planId.Value);
        if (plan == null) return (false, "Plan no encontrado.", null);

        var parent = await _context.Users.FindAsync(parentId);
        if (parent == null) return (false, "Padre no encontrado.", null);

        if (parent.Credits < plan.CreditCost)
            return (false, "Créditos insuficientes.", null);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = userName,
            PasswordHash = BC.HashPassword(password),
            Role = UserRole.Customer,
            ParentId = parentId,
            Credits = 0,
            IsActive = true,
            PlanId = planId,
            ServiceExpiry = DateTime.UtcNow.AddHours(plan.DurationHours),
            // Marcar como trial si el plan es de costo 0
            IsTrial = plan.CreditCost == 0,
            TrialExpiry = plan.CreditCost == 0 ? DateTime.UtcNow.AddHours(plan.DurationHours) : null
        };

        _context.Users.Add(user);
        parent.Credits -= plan.CreditCost;

        _context.CreditTransactions.Add(new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceUserId = parentId,
            TargetUserId = user.Id,
            Amount = plan.CreditCost,
            Type = CreditTransactionType.PlanPurchase,
            CreatedAt = DateTime.UtcNow,
            Note = $"Creación de usuario {userName}"
        });

        await _context.SaveChangesAsync();
        return (true, "Usuario creado exitosamente.", user.Id);
    }

    public async Task<(bool Success, string Message)> ExtendServiceAsync(Guid userId, Guid tenantId, int days, Guid? nodeId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null) return (false, "Usuario no encontrado.");

        user.ServiceExpiry = (user.ServiceExpiry ?? DateTime.UtcNow).AddDays(days);
        await _context.SaveChangesAsync();
        return (true, "Servicio extendido.");
    }

    public async Task<(bool Success, string Message)> RemoveUserFromVpsAsync(Guid userId, Guid tenantId, Guid nodeId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null) return (false, "Usuario no encontrado.");

        user.IsProvisionedOnVps = false;
        await _context.SaveChangesAsync();
        return (true, "Usuario removido del VPS.");
    }

    public async Task<(bool Success, string Message)> ProvisionNewUserAsync(Guid userId, Guid nodeId, Guid adminId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return (false, "Usuario no encontrado.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null) return (false, "Nodo no encontrado.");

        var transaction = new CreditTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = user.TenantId,
            SourceUserId = adminId,
            TargetUserId = userId,
            Amount = 0,
            Type = CreditTransactionType.UserCreation,
            CreatedAt = DateTime.UtcNow
        };

        _context.CreditTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        return (true, "Proceso completado.");
    }
}