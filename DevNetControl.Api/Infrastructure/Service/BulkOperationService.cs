using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio para operaciones masivas (CSV) de usuarios.
/// </summary>
public class BulkOperationService
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;
    private readonly NotificationService _notificationService;

    public BulkOperationService(
        ApplicationDbContext context,
        CreditService creditService,
        NotificationService notificationService)
    {
        _context = context;
        _creditService = creditService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Procesa un archivo CSV para creación masiva de usuarios.
    /// Formato esperado: UserName,Password,PlanId (una por línea)
    /// </summary>
    public async Task<(int Created, int Failed, List<string> Errors)> BulkCreateUsersAsync(
        Guid parentId, Guid tenantId, Stream csvStream)
    {
        var created = 0;
        var failed = 0;
        var errors = new List<string>();

        using var reader = new StreamReader(csvStream);
        string? line;
        var lineNumber = 0;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 3)
            {
                failed++;
                errors.Add($"Línea {lineNumber}: Formato inválido (esperado: UserName,Password,PlanId)");
                continue;
            }

            var userName = parts[0].Trim();
            var password = parts[1].Trim();
            if (!Guid.TryParse(parts[2].Trim(), out var planId))
            {
                failed++;
                errors.Add($"Línea {lineNumber}: PlanId inválido '{parts[2]}'");
                continue;
            }

            // Reutilizar lógica de UserProvisioningService (simulada aquí por simplicidad)
            var result = await CreateUserInternalAsync(parentId, tenantId, userName, password, planId, null);
            if (result.Success)
            {
                created++;
            }
            else
            {
                failed++;
                errors.Add($"Línea {lineNumber}: {result.Message}");
            }
        }

        return (created, failed, errors);
    }

    /// <summary>
    /// Lógica interna simplificada para crear usuario (basada en UserProvisioningService.CreateUserAsync)
    /// </summary>
    private async Task<(bool Success, string Message)> CreateUserInternalAsync(
        Guid parentId, Guid tenantId, string userName, string password, Guid planId, Guid? nodeId)
    {
        if (await _context.Users.AnyAsync(u => u.UserName == userName && u.TenantId == tenantId))
            return (false, "El usuario ya existe.");

        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null) return (false, "Plan no encontrado.");

        var parent = await _context.Users.FindAsync(parentId);
        if (parent == null) return (false, "Padre no encontrado.");

        if (parent.Credits < plan.CreditCost)
            return (false, "Créditos insuficientes.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = userName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer,
            ParentId = parentId,
            Credits = 0,
            IsActive = true,
            PlanId = planId,
            ServiceExpiry = DateTime.UtcNow.AddHours(plan.DurationHours)
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
            Note = $"Creación masiva de usuario {userName}"
        });

        await _context.SaveChangesAsync();

        // Verificar créditos bajos tras creación
        if (parent.Credits <= 10m)
            await _notificationService.GenerateLowCreditAlertsAsync();

        return (true, "Usuario creado exitosamente.");
    }

    /// <summary>
    /// Extiende el servicio de múltiples usuarios en lote.
    /// </summary>
    public async Task<(int SuccessCount, int FailCount, List<string> Errors)> BulkExtendServiceAsync(
        Guid parentId, Guid tenantId, List<Guid> userIds, int days)
    {
        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();

        foreach (var userId in userIds)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
            if (user == null)
            {
                failCount++;
                errors.Add($"Usuario {userId} no encontrado.");
                continue;
            }

            // Verificar jerarquía: solo el padre directo o Admin puede extender
            var parent = await _context.Users.FindAsync(parentId);
            if (parent == null)
            {
                failCount++;
                errors.Add($"Padre {parentId} no encontrado.");
                continue;
            }

            if (parent.Role != UserRole.Admin && parent.Role != UserRole.SuperAdmin && user.ParentId != parentId)
            {
                failCount++;
                errors.Add($"Sin permisos para extender servicio de {user.UserName}.");
                continue;
            }

            user.ServiceExpiry = (user.ServiceExpiry ?? DateTime.UtcNow).AddDays(days);
            successCount++;
        }

        await _context.SaveChangesAsync();
        return (successCount, failCount, errors);
    }

    /// <summary>
    /// Elimina múltiples usuarios en lote.
    /// </summary>
    public async Task<(int SuccessCount, int FailCount, List<string> Errors)> BulkDeleteAsync(
        Guid parentId, Guid tenantId, List<Guid> userIds)
    {
        var successCount = 0;
        var failCount = 0;
        var errors = new List<string>();

        foreach (var userId in userIds)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
            if (user == null)
            {
                failCount++;
                errors.Add($"Usuario {userId} no encontrado.");
                continue;
            }

            // Verificar jerarquía
            var parent = await _context.Users.FindAsync(parentId);
            if (parent == null)
            {
                failCount++;
                errors.Add($"Padre {parentId} no encontrado.");
                continue;
            }

            if (parent.Role != UserRole.Admin && parent.Role != UserRole.SuperAdmin && user.ParentId != parentId)
            {
                failCount++;
                errors.Add($"Sin permisos para eliminar a {user.UserName}.");
                continue;
            }

            _context.Users.Remove(user);
            successCount++;
        }

        await _context.SaveChangesAsync();
        return (successCount, failCount, errors);
    }
}
