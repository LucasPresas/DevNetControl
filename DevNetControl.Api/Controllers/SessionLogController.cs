using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Dtos;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionLogController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SessionLogController> _logger;

    public SessionLogController(ApplicationDbContext context, ILogger<SessionLogController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el historial de logs de sesión paginado
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSessionLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new { Message = "Parámetros de paginación inválidos." });

        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var query = _context.SessionLogs
            .Where(sl => sl.TenantId == tenantId)
            .AsQueryable();

        // Búsqueda opcional
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.UserName.Contains(search) ||
                s.ClientIp.Contains(search) ||
                s.Action.Contains(search) ||
                s.Details.Contains(search) ||
                s.NodeIp.Contains(search));
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(sl => sl.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sl => new SessionLogDto(
                sl.Id,
                sl.UserId,
                sl.UserName,
                sl.ClientIp,
                sl.NodeIp,
                sl.Action,
                sl.Details,
                sl.Timestamp
            ))
            .ToListAsync();

        return Ok(new
        {
            Data = logs,
            Pagination = new
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (totalCount + pageSize - 1) / pageSize
            }
        });
    }

    /// <summary>
    /// Obtiene el detalle de un log de sesión específico
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSessionLogById(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var log = await _context.SessionLogs
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == tenantId);

        if (log == null)
            return NotFound(new { Message = "Log de sesión no encontrado." });

        var dto = new SessionLogDto(
            log.Id,
            log.UserId,
            log.UserName,
            log.ClientIp,
            log.NodeIp,
            log.Action,
            log.Details,
            log.Timestamp
        );

        return Ok(dto);
    }

    /// <summary>
    /// Registra una nueva entrada de log de sesión
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSessionLog([FromBody] CreateSessionLogRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var userName = ClaimsHelper.GetCurrentUserName(User);
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var sessionLog = new SessionLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
            ClientIp = clientIp,
            NodeIp = request.NodeIp ?? string.Empty,
            Action = request.Action,
            Details = request.Details,
            Timestamp = DateTime.UtcNow
        };

        _context.SessionLogs.Add(sessionLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session log created: {Action} by {UserName} (IP: {ClientIp})", 
            request.Action, userName, clientIp);

        return CreatedAtAction(nameof(GetSessionLogById), new { id = sessionLog.Id }, new
        {
            Message = "Log de sesión registrado exitosamente.",
            Id = sessionLog.Id
        });
    }

    /// <summary>
    /// Obtiene los logs de sesión activos de un usuario específico
    /// Solo Admin o el usuario mismo pueden verlos
    /// </summary>
    [HttpGet("user/{userId}/active")]
    public async Task<IActionResult> GetActiveSessionsByUser(Guid userId)
    {
        var currentUserId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        // Solo Admin o el usuario mismo puede ver sus logs
        if (currentUserId != userId && role != "Admin" && role != "SuperAdmin")
            return Forbid();

        // Verificar que el usuario existe y pertenece al tenant
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado." });

        var logs = await _context.SessionLogs
            .Where(sl => sl.UserId == userId && sl.TenantId == tenantId)
            .OrderByDescending(sl => sl.Timestamp)
            .Take(100)
            .Select(sl => new SessionLogDto(
                sl.Id,
                sl.UserId,
                sl.UserName,
                sl.ClientIp,
                sl.NodeIp,
                sl.Action,
                sl.Details,
                sl.Timestamp
            ))
            .ToListAsync();

        return Ok(new { Data = logs });
    }

    /// <summary>
    /// Obtiene estadísticas de logs de sesión
    /// Solo Admin puede acceder
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetSessionLogStats()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var today = DateTime.UtcNow.Date;

        var totalLogs = await _context.SessionLogs
            .CountAsync(sl => sl.TenantId == tenantId);

        var uniqueUsers = await _context.SessionLogs
            .Where(sl => sl.TenantId == tenantId)
            .Select(sl => sl.UserId)
            .Distinct()
            .CountAsync();

        var logsLast24Hours = await _context.SessionLogs
            .CountAsync(sl => sl.TenantId == tenantId && 
                             sl.Timestamp >= DateTime.UtcNow.AddHours(-24));

        var logsToday = await _context.SessionLogs
            .CountAsync(sl => sl.TenantId == tenantId && sl.Timestamp >= today);

        var actionCounts = await _context.SessionLogs
            .Where(sl => sl.TenantId == tenantId)
            .GroupBy(sl => sl.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return Ok(new
        {
            TotalLogs = totalLogs,
            UniqueUsers = uniqueUsers,
            LogsLast24Hours = logsLast24Hours,
            LogsToday = logsToday,
            TopActions = actionCounts,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Elimina un log de sesión específico
    /// Solo Admin puede eliminar
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteSessionLog(Guid id)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var log = await _context.SessionLogs
            .FirstOrDefaultAsync(sl => sl.Id == id && sl.TenantId == tenantId);

        if (log == null)
            return NotFound(new { Message = "Log de sesión no encontrado." });

        _context.SessionLogs.Remove(log);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Session log deleted: {Id}", id);

        return Ok(new { Message = "Log de sesión eliminado exitosamente." });
    }

    /// <summary>
    /// Limpia los logs de sesión más antiguos que N días
    /// Solo Admin puede ejecutar
    /// </summary>
    [HttpPost("cleanup")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> CleanupOldLogs([FromQuery] int olderThanDays = 30)
    {
        if (olderThanDays < 1 || olderThanDays > 365)
            return BadRequest(new { Message = "El rango debe estar entre 1 y 365 días." });

        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);

        var logsToDelete = await _context.SessionLogs
            .Where(sl => sl.TenantId == tenantId && sl.Timestamp < cutoffDate)
            .ToListAsync();

        _context.SessionLogs.RemoveRange(logsToDelete);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old session logs (older than {Days} days)", logsToDelete.Count, olderThanDays);

        return Ok(new
        {
            Message = $"Se eliminaron {logsToDelete.Count} logs de sesión.",
            DeletedCount = logsToDelete.Count
        });
    }
}
