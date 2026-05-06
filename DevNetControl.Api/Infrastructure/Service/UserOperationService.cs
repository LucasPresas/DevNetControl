using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Dtos;

namespace DevNetControl.Api.Infrastructure.Services;

public class UserOperationService
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;
    private readonly ActivityLogService _activityLogService;

    public UserOperationService(
        ApplicationDbContext context, 
        CreditService creditService, 
        ActivityLogService activityLogService)
    {
        _context = context;
        _creditService = creditService;
        _activityLogService = activityLogService;
    }

    // Debug: Método para verificar estado antes de operación
    public async Task<(bool Success, string Message, int NewMaxConnections, decimal RemainingCredits)> 
        AddConnectionsAsync(Guid targetUserId, Guid actorUserId, string actorRole, string actorUserName, Guid tenantId, int connectionsToAdd)
    {
        Console.WriteLine($"[DEBUG] AddConnectionsAsync - Target: {targetUserId}, Actor: {actorUserId}, Amount: {connectionsToAdd}");
        
        var targetUser = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == targetUserId && u.TenantId == tenantId);

        if (targetUser == null)
            return (false, "Usuario no encontrado.", 0, 0);

        // Actualizar conexiones del usuario objetivo
        targetUser.AdditionalConnections += connectionsToAdd;
        var newMax = (targetUser.Plan?.MaxConnections ?? 0) + targetUser.AdditionalConnections;
        
        // Admin/SuperAdmin no consumen créditos
        if (actorRole == "Admin" || actorRole == "SuperAdmin")
        {
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] Admin/SA - No credits deducted. Target connections now: {newMax}");
            
            await _activityLogService.LogCreditsConsumedAsync(
                actorUserId, targetUserId, targetUser.UserName,
                tenantId, actorRole, actorUserName,
                connectionsToAdd, targetUser.Credits, targetUser.Credits,
                $"Agregadas {connectionsToAdd} conexión(es) a {targetUser.UserName} (Admin)");
                
            return (true, $"Se agregaron {connectionsToAdd} conexión(es). Créditos gastados: 0 (Admin)", 
                newMax, targetUser.Credits);
        }

        // Para Reseller/SubReseller: verificar y deducir créditos del actor
        var actorUser = await _context.Users.FindAsync(actorUserId);
        if (actorUser == null) 
            return (false, "Actor no encontrado.", 0, 0);

        Console.WriteLine($"[DEBUG] Actor credits BEFORE: {actorUser.Credits}");
        
        if (actorUser.Credits < connectionsToAdd)
            return (false, $"Créditos insuficientes. Tienes {actorUser.Credits} créditos y se requieren {connectionsToAdd} créditos.", 
                newMax, actorUser.Credits);

        var creditsBefore = actorUser.Credits;
        actorUser.Credits -= connectionsToAdd;
        
        Console.WriteLine($"[DEBUG] Actor credits AFTER: {actorUser.Credits}");
        
        await _context.SaveChangesAsync();

        await _activityLogService.LogCreditsConsumedAsync(
            actorUserId, targetUserId, targetUser.UserName,
            tenantId, actorRole, actorUserName,
            connectionsToAdd, creditsBefore, actorUser.Credits,
            $"Agregadas {connectionsToAdd} conexión(es) a {targetUser.UserName}");

        return (true, $"Se agregaron {connectionsToAdd} conexión(es). Créditos gastados: {connectionsToAdd}", 
            newMax, actorUser.Credits);
    }

    public async Task<(bool Success, string Message, string PlanName, DateTime? ServiceExpiry, decimal RemainingCredits)> 
        RenewPlanAsync(Guid targetUserId, Guid actorUserId, string actorRole, string actorUserName, Guid tenantId, Guid planId, int durationHours)
    {
        Console.WriteLine($"[DEBUG] RenewPlanAsync - Target: {targetUserId}, Plan: {planId}, Days: {durationHours}");
        
        var targetUser = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == targetUserId && u.TenantId == tenantId);

        if (targetUser == null)
            return (false, "Usuario no encontrado.", "", null, 0);

        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
            return (false, "Plan no encontrado.", "", null, 0);

        // Obtener actor para ambos casos (Admin y Reseller)
        var actorUser = await _context.Users.FindAsync(actorUserId);
        if (actorUser == null) 
            return (false, "Actor no encontrado.", "", null, 0);

        // Admin/SuperAdmin no consumen créditos
        if (actorRole == "Admin" || actorRole == "SuperAdmin")
        {
            targetUser.PlanId = plan.Id;
            // Sumar tiempo a la fecha actual de expiración (si existe), sino usar DateTime.UtcNow
            targetUser.ServiceExpiry = (targetUser.ServiceExpiry ?? DateTime.UtcNow).AddHours(durationHours > 0 ? durationHours : plan.DurationHours);
            
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[DEBUG] Admin/SA - Plan renewed. New expiry: {targetUser.ServiceExpiry}");
            
            await _activityLogService.LogCreditsConsumedAsync(
                actorUserId, targetUserId, targetUser.UserName,
                tenantId, actorRole, actorUserName,
                plan.CreditCost, actorUser.Credits, actorUser.Credits,
                $"Plan {plan.Name} renovado para {targetUser.UserName}. Duración: {durationHours} horas (Admin)");
                
            return (true, $"Plan {plan.Name} renovado exitosamente. Expira: {targetUser.ServiceExpiry}", 
                plan.Name, targetUser.ServiceExpiry, actorUser.Credits);
        }

        // Para Reseller/SubReseller: verificar y deducir créditos del actor

        Console.WriteLine($"[DEBUG] Actor credits BEFORE: {actorUser.Credits}, Plan cost: {plan.CreditCost}");
        
        if (actorUser.Credits < plan.CreditCost)
            return (false, $"Créditos insuficientes. Tienes {actorUser.Credits} créditos y se requieren {plan.CreditCost} créditos.", 
                "", null, actorUser.Credits);

        var creditsBefore = actorUser.Credits;
        actorUser.Credits -= plan.CreditCost;
        
        // Actualizar el usuario objetivo (targetUser) con nuevo plan y fecha
        targetUser.PlanId = plan.Id;
        // Sumar tiempo a la fecha actual de expiración (si existe), sino usar DateTime.UtcNow
        targetUser.ServiceExpiry = (targetUser.ServiceExpiry ?? DateTime.UtcNow).AddHours(durationHours > 0 ? durationHours : plan.DurationHours);
        
        Console.WriteLine($"[DEBUG] Actor credits AFTER: {actorUser.Credits}, Target expiry: {targetUser.ServiceExpiry}");
        
        await _context.SaveChangesAsync();

        await _activityLogService.LogCreditsConsumedAsync(
            actorUserId, targetUserId, targetUser.UserName,
            tenantId, actorRole, actorUserName,
            plan.CreditCost, creditsBefore, actorUser.Credits,
            $"Plan {plan.Name} renovado para {targetUser.UserName}. Duración: {durationHours} horas");

        return (true, $"Plan {plan.Name} renovado exitosamente. Expira: {targetUser.ServiceExpiry}", 
            plan.Name, targetUser.ServiceExpiry, actorUser.Credits);
    }

    public async Task<(bool Success, string Message)> 
        ExtendServiceAsync(Guid targetUserId, Guid tenantId, int days, Guid? nodeId)
    {
        Console.WriteLine($"[DEBUG] ExtendServiceAsync - Target: {targetUserId}, Days: {days}");
        
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == targetUserId && u.TenantId == tenantId);

        if (targetUser == null) 
            return (false, "Usuario no encontrado.");

        targetUser.ServiceExpiry = (targetUser.ServiceExpiry ?? DateTime.UtcNow).AddDays(days);
        
        Console.WriteLine($"[DEBUG] Service extended. New expiry: {targetUser.ServiceExpiry}");
        
        await _context.SaveChangesAsync();
        return (true, "Servicio extendido.");
    }
}
