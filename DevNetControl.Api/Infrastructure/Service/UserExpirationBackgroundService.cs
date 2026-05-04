using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

public class UserExpirationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserExpirationBackgroundService> _logger;

    public UserExpirationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<UserExpirationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de expiracion de usuarios iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var sshUserManager = scope.ServiceProvider.GetRequiredService<SshUserManager>();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

                await ProcessExpiredUsersAsync(context, sshUserManager, notificationService, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de expiracion de usuarios.");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        _logger.LogInformation("Servicio de expiracion de usuarios detenido.");
    }

    private async Task ProcessExpiredUsersAsync(
        ApplicationDbContext context,
        SshUserManager sshUserManager,
        NotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expiredTrialUsers = await context.Users
            .Where(u => u.IsTrial && u.TrialExpiry.HasValue && u.TrialExpiry.Value <= now)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Usuarios de prueba expirados encontrados: {Count}", expiredTrialUsers.Count);

        foreach (var user in expiredTrialUsers)
        {
            try
            {
                var node = await context.VpsNodes
                    .FirstOrDefaultAsync(n => n.TenantId == user.TenantId && n.OwnerId == user.ParentId, cancellationToken);

                if (node != null && user.IsProvisionedOnVps)
                {
                    var sshResult = await sshUserManager.DeleteUserFromVpsAsync(node.Id, user.UserName);

                    if (sshResult.Success)
                    {
                        _logger.LogInformation("Usuario de prueba {Username} eliminado del VPS {NodeIp}", user.UserName, node.IP);
                    }
                    else
                    {
                        _logger.LogWarning("Error eliminando usuario {Username} del VPS: {Error}", user.UserName, sshResult.Error);
                    }
                }

                // Notificar expiración
                context.Notifications.Add(new Notification
                {
                    Type = "Expired",
                    Message = $"Tu cuenta de prueba ha expirado el {now:yyyy-MM-dd}.",
                    UserId = user.Id,
                    TenantId = user.TenantId
                });

                var subordinates = await context.Users
                    .Where(u => u.ParentId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var sub in subordinates)
                {
                    sub.ParentId = null;
                }

                var sessions = await context.SessionLogs
                    .Where(s => s.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                foreach (var session in sessions)
                {
                    session.UserId = null;
                }

                var nodeAccesses = await context.NodeAccesses
                    .Where(na => na.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                context.NodeAccesses.RemoveRange(nodeAccesses);

                var planAccesses = await context.PlanAccesses
                    .Where(pa => pa.UserId == user.Id)
                    .ToListAsync(cancellationToken);

                context.PlanAccesses.RemoveRange(planAccesses);

                await context.SaveChangesAsync(cancellationToken);

                context.Users.Remove(user);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Usuario de prueba {Username} expirado y eliminado de la base de datos.", user.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando expiracion del usuario {Username}.", user.UserName);
            }
        }

        var expiredPaidUsers = await context.Users
            .Where(u => !u.IsTrial && u.ServiceExpiry.HasValue && u.ServiceExpiry.Value <= now && u.Role == UserRole.Customer)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Usuarios pagos expirados encontrados: {Count}", expiredPaidUsers.Count);

        foreach (var user in expiredPaidUsers)
        {
            try
            {
                var node = await context.VpsNodes
                    .FirstOrDefaultAsync(n => n.TenantId == user.TenantId && n.OwnerId == user.ParentId, cancellationToken);

                if (node != null && user.IsProvisionedOnVps)
                {
                    var sshResult = await sshUserManager.DeleteUserFromVpsAsync(node.Id, user.UserName);

                    if (sshResult.Success)
                    {
                        _logger.LogInformation("Usuario pago {Username} eliminado del VPS {NodeIp}", user.UserName, node.IP);
                    }
                    else
                    {
                        _logger.LogWarning("Error eliminando usuario pago {Username} del VPS: {Error}", user.UserName, sshResult.Error);
                    }
                }

                // Notificar expiración
                context.Notifications.Add(new Notification
                {
                    Type = "Expired",
                    Message = $"Tu servicio expiró el {now:yyyy-MM-dd}.",
                    UserId = user.Id,
                    TenantId = user.TenantId
                });

                user.IsProvisionedOnVps = false;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Usuario pago {Username} marcado como expirado.", user.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando expiracion del usuario pago {Username}.", user.UserName);
            }
        }
    }
}
