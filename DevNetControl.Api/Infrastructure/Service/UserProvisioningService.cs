using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
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

    public async Task<(bool Success, string Message, Guid? UserId)> CreateUserAsync(
        Guid creatorId, Guid tenantId, string userName, string password, Guid planId, Guid? targetNodeId = null)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == userName && u.TenantId == tenantId))
            return (false, "El nombre de usuario ya existe.", null);

        var plan = await _context.Plans.FirstOrDefaultAsync(p => p.Id == planId && p.TenantId == tenantId);
        if (plan == null)
            return (false, "Plan no encontrado.", null);

        if (!plan.IsActive)
            return (false, "El plan seleccionado no esta activo.", null);

        var creator = await _context.Users.FindAsync(creatorId);
        if (creator == null)
            return (false, "Usuario creador no encontrado.", null);

        if (creator.TenantId != tenantId)
            return (false, "El creador no pertenece a este tenant.", null);

        decimal creditCost = plan.CreditCost;

        if (creator.Role != UserRole.Admin && creator.Role != UserRole.SuperAdmin)
        {
            if (creator.Credits < creditCost)
                return (false, $"Creditos insuficientes. El plan cuesta {creditCost} creditos. Tienes {creator.Credits}.", null);

            if (plan.MaxDevices > creator.MaxDevices && creator.Role != UserRole.Admin)
                return (false, $"No tienes permiso para crear usuarios con {plan.MaxDevices} dispositivos.", null);
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = userName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer,
            Credits = 0,
            ParentId = creatorId,
            PlanId = planId,
            MaxDevices = plan.MaxDevices,
            ServiceExpiry = DateTime.UtcNow.AddHours(plan.DurationHours),
            IsTrial = plan.IsTrial,
            TrialExpiry = plan.IsTrial ? DateTime.UtcNow.AddHours(2) : null,
            IsProvisionedOnVps = false,
            IsActive = true
        };

        if (plan.IsTrial)
        {
            var tenant = await _context.Tenants.FindAsync(tenantId);
            if (tenant == null || !tenant.IsActive)
                return (false, "Tenant no encontrado o inactivo.", null);

            var trialUsersCount = await _context.Users.CountAsync(u =>
                u.TenantId == tenantId && u.ParentId == creatorId && u.IsTrial);

            if (creator.Role != UserRole.Admin && creator.Role != UserRole.SuperAdmin)
            {
                if (trialUsersCount >= tenant.TrialMaxPerReseller)
                    return (false, $"Limite de usuarios de prueba alcanzado ({tenant.TrialMaxPerReseller}).", null);
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (creator.Role != UserRole.Admin && creator.Role != UserRole.SuperAdmin && creditCost > 0)
            {
                creator.Credits -= creditCost;

                var costTransaction = new CreditTransaction
                {
                    TenantId = tenantId,
                    FromUserId = creatorId,
                    ToUserId = newUser.Id,
                    Amount = creditCost,
                    Note = $"Creacion de usuario {userName} - Plan: {plan.Name} ({plan.MaxDevices} disp, {FormatDuration(plan.DurationHours)})",
                    Type = CreditTransactionType.UserCreationCost
                };

                _context.CreditTransactions.Add(costTransaction);
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            if (targetNodeId.HasValue)
            {
                var node = await _context.VpsNodes.FindAsync(targetNodeId.Value);
                if (node == null || node.TenantId != tenantId)
                    return (false, "Nodo VPS no encontrado o no pertenece al tenant.", null);

                var sshResult = await _sshUserManager.CreateUserOnVpsAsync(
                    targetNodeId.Value, userName, password, plan.MaxDevices, newUser.ServiceExpiry);

                if (!sshResult.Success)
                {
                    if (creator.Role != UserRole.Admin && creator.Role != UserRole.SuperAdmin && creditCost > 0)
                    {
                        creator.Credits += creditCost;
                        await _context.SaveChangesAsync();
                    }

                    await transaction.RollbackAsync();
                    return (false, $"Error provisionando en VPS: {sshResult.Error}. Los creditos fueron reembolsados.", null);
                }

                newUser.IsProvisionedOnVps = true;
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return (true, $"Usuario {userName} creado con plan {plan.Name}. Costo: {creditCost} creditos.", newUser.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error interno: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> ExtendServiceAsync(
        Guid userId, Guid tenantId, int additionalDays, Guid? targetNodeId = null)
    {
        if (additionalDays <= 0)
            return (false, "Los dias adicionales deben ser mayores a cero.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return (false, "Usuario no encontrado.");

        if (user.IsTrial)
            return (false, "No se puede extender un usuario de prueba.");

        var plan = user.PlanId.HasValue
            ? await _context.Plans.FindAsync(user.PlanId.Value)
            : null;

        decimal creditCost = plan?.CreditCost ?? (user.MaxDevices * 1m);

        var planDurationDays = plan != null ? (int)Math.Ceiling((double)plan.DurationHours / 24) : 0;

        if (additionalDays != planDurationDays)
        {
            creditCost = user.MaxDevices * (additionalDays / 30m);
            if (creditCost <= 0)
                creditCost = user.MaxDevices;
        }

        var parent = await _context.Users.FindAsync(user.ParentId);
        if (parent == null)
            return (false, "No se encontro el usuario padre para cobrar la extension.");

        if (parent.Credits < creditCost)
            return (false, $"Creditos insuficientes del padre. Se necesitan {creditCost}, tiene {parent.Credits}.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            parent.Credits -= creditCost;

            var baseExpiry = user.ServiceExpiry ?? DateTime.UtcNow;
            user.ServiceExpiry = baseExpiry.AddDays(additionalDays);

            var costTransaction = new CreditTransaction
            {
                TenantId = tenantId,
                FromUserId = parent.Id,
                ToUserId = user.Id,
                Amount = creditCost,
                Note = $"Extension de servicio para {user.UserName} (+{additionalDays} dias)",
                Type = CreditTransactionType.ServiceExtension
            };

            _context.CreditTransactions.Add(costTransaction);
            await _context.SaveChangesAsync();

            if (targetNodeId.HasValue && user.IsProvisionedOnVps)
            {
                var node = await _context.VpsNodes.FindAsync(targetNodeId.Value);
                if (node != null && node.TenantId == tenantId)
                {
                    var sshResult = await _sshUserManager.ExtendUserExpiryOnVpsAsync(
                        targetNodeId.Value, user.UserName, user.ServiceExpiry.Value);

                    if (!sshResult.Success)
                    {
                        parent.Credits += creditCost;
                        user.ServiceExpiry = baseExpiry;
                        await _context.SaveChangesAsync();

                        await transaction.RollbackAsync();
                        return (false, $"Error extendiendo en VPS: {sshResult.Error}. Creditos reembolsados.");
                    }
                }
            }

            await transaction.CommitAsync();
            return (true, $"Servicio extendido {additionalDays} dias. Costo: {creditCost} creditos.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Error interno: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RemoveUserFromVpsAsync(
        Guid userId, Guid tenantId, Guid nodeId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.TenantId != tenantId)
            return (false, "Usuario no encontrado.");

        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null || node.TenantId != tenantId)
            return (false, "Nodo no encontrado.");

        if (!user.IsProvisionedOnVps)
            return (false, "El usuario no esta provisionado en ningun VPS.");

        var sshResult = await _sshUserManager.DeleteUserFromVpsAsync(nodeId, user.UserName);

        if (!sshResult.Success)
            return (false, $"Error eliminando del VPS: {sshResult.Error}");

        user.IsProvisionedOnVps = false;
        user.ServiceExpiry = null;
        await _context.SaveChangesAsync();

        return (true, $"Usuario {user.UserName} eliminado del VPS exitosamente.");
    }

    private static string FormatDuration(int hours)
    {
        if (hours >= 24 && hours % 24 == 0)
            return $"{hours / 24} dias";
        return $"{hours} horas";
    }
}
