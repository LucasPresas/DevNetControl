using System;
using System.ComponentModel.DataAnnotations;

namespace DevNetControl.Api.Domain;

/// <summary>
/// Registro de auditoría para acciones administrativas y eventos sensibles.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Ej: "UserCreated", "PlanChanged"
    
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty; // Detalles de la acción
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;
    
    // Relación con el usuario que realizó la acción
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    
    // Tenant para aislamiento multi-tenant
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
