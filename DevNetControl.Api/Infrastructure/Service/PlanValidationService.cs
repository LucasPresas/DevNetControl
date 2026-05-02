using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio para validar restricciones de planes (MaxConnections, MaxDevices) en tiempo real.
/// </summary>
public class PlanValidationService
{
    private readonly ApplicationDbContext _context;

    public PlanValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Valida si un usuario puede realizar una nueva conexion (basado en MaxConnections).
    /// </summary>
    public async Task<(bool IsAllowed, string Message)> ValidateConnectionAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Plan == null)
            return (false, "Usuario o plan no encontrado.");

        if (!user.Plan.IsActive)
            return (false, "Plan inactivo.");

        // Contar conexiones activas via SessionLog
        var activeConnections = await _context.SessionLogs
            .CountAsync(s => s.UserId == userId && s.Action == "Connected");

        if (activeConnections >= user.Plan.MaxConnections)
            return (false, $"Límite de conexiones alcanzado ({user.Plan.MaxConnections} máximo).");

        return (true, "Conexión permitida.");
    }

    /// <summary>
    /// Valida si un usuario puede registrar un nuevo dispositivo (basado en MaxDevices).
    /// </summary>
    public async Task<(bool IsAllowed, string Message)> ValidateDeviceAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Plan == null)
            return (false, "Usuario o plan no encontrado.");

        if (!user.Plan.IsActive)
            return (false, "Plan inactivo.");

        // Asumimos que los dispositivos se registran via SessionLog con Action "DeviceRegistered"
        var registeredDevices = await _context.SessionLogs
            .Where(s => s.UserId == userId && s.Action == "DeviceRegistered")
            .Select(s => s.ClientIp) // IP como identificador simple de dispositivo
            .Distinct()
            .CountAsync();

        if (registeredDevices >= user.Plan.MaxDevices)
            return (false, $"Límite de dispositivos alcanzado ({user.Plan.MaxDevices} máximo).");

        return (true, "Dispositivo permitido.");
    }

    /// <summary>
    /// Obtener información de límites del plan de un usuario.
    /// </summary>
    public async Task<(int MaxConnections, int MaxDevices, int ActiveConnections, int RegisteredDevices)> GetUserLimitsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Plan)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Plan == null)
            return (0, 0, 0, 0);

        var activeConnections = await _context.SessionLogs
            .CountAsync(s => s.UserId == userId && s.Action == "Connected");

        var registeredDevices = await _context.SessionLogs
            .Where(s => s.UserId == userId && s.Action == "DeviceRegistered")
            .Select(s => s.ClientIp)
            .Distinct()
            .CountAsync();

        return (user.Plan.MaxConnections, user.Plan.MaxDevices, activeConnections, registeredDevices);
    }
}
