using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio centralizado para registrar acciones administrativas en AuditLog.
/// </summary>
public class AuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string description, Guid? userId = null, Guid? tenantId = null)
    {
        // Obtener IP del cliente
        var ipAddress = GetClientIpAddress();
        
        // Si no se pasan, intentar obtener del HttpContext
        if (userId == null)
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("UserId")?.Value;
            if (userIdClaim != null) userId = Guid.Parse(userIdClaim);
        }

        if (tenantId == null)
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            if (tenantIdClaim != null) tenantId = Guid.Parse(tenantIdClaim);
            else tenantId = Guid.Empty; // Valor por defecto
        }

        var log = new AuditLog
        {
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            UserId = userId,
            TenantId = tenantId.Value
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private string GetClientIpAddress()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return "unknown";

        // Verificar headers de proxy
        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').First()?.Trim();
        if (string.IsNullOrEmpty(ip))
            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (string.IsNullOrEmpty(ip))
            ip = context.Connection.RemoteIpAddress?.ToString();

        return ip ?? "unknown";
    }
}
