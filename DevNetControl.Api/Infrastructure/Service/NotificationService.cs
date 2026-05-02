using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio para gestión de notificaciones: generación y consulta.
/// </summary>
public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Genera alertas de crédito bajo para usuarios con créditos <= threshold.
    /// </summary>
    public async Task GenerateLowCreditAlertsAsync(decimal threshold = 10m)
    {
        var users = await _context.Users
            .Where(u => u.Credits <= threshold && u.IsActive)
            .ToListAsync();

        foreach (var user in users)
        {
            // Verificar si ya existe notificación no leída del mismo tipo
            var existing = await _context.Notifications
                .AnyAsync(n => n.UserId == user.Id && n.Type == "LowCredit" && !n.IsRead);

            if (!existing)
            {
                _context.Notifications.Add(new Notification
                {
                    Type = "LowCredit",
                    Message = $"Créditos bajos: {user.Credits} restantes.",
                    UserId = user.Id,
                    TenantId = user.TenantId
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Genera alertas de expiración cercana (próximos N días).
    /// </summary>
    public async Task GenerateExpirationWarningsAsync(int daysBefore = 3)
    {
        var warningDate = DateTime.UtcNow.AddDays(daysBefore);
        
        var users = await _context.Users
            .Where(u => u.ServiceExpiry <= warningDate && u.ServiceExpiry > DateTime.UtcNow && u.IsActive)
            .ToListAsync();

        foreach (var user in users)
        {
            var existing = await _context.Notifications
                .AnyAsync(n => n.UserId == user.Id && n.Type == "ExpirationWarning" && !n.IsRead);

            if (!existing)
            {
                _context.Notifications.Add(new Notification
                {
                    Type = "ExpirationWarning",
                    Message = $"Servicio expira el {user.ServiceExpiry:yyyy-MM-dd}.",
                    UserId = user.Id,
                    TenantId = user.TenantId
                });
            }
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Obtener notificaciones de un usuario (paginado).
    /// </summary>
    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications
            .Where(n => n.UserId == userId);

        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Marcar notificación como leída.
    /// </summary>
    public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}
