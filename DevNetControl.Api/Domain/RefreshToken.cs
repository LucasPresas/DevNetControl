using System;

namespace DevNetControl.Api.Domain;

/// <summary>
/// Token de refresco para renovar JWT sin re-autenticación.
/// Almacenado de forma segura vinculado al usuario.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public bool IsUsed { get; set; } = false;
    public bool IsRevoked { get; set; } = false;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
