using System;
using System.ComponentModel.DataAnnotations;

namespace DevNetControl.Api.Domain;

/// <summary>
/// Notificaciones para usuarios: crédito bajo, expiración de servicio, etc.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // "LowCredit", "ExpirationWarning"
    
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Relación con usuario
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    // Aislamiento multi-tenant
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
