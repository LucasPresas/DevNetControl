namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Define una política de rate limiting para un endpoint específico.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>
    /// Identificador único de la política (ej: "auth-login", "user-create")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Número máximo de requests permitidos
    /// </summary>
    public int MaxRequests { get; set; } = 5;

    /// <summary>
    /// Ventana de tiempo en segundos
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Mensaje de error personalizado
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Identificador de rate limit (ip, userId, combinación, etc)
    /// </summary>
    public RateLimitIdentifier Identifier { get; set; } = RateLimitIdentifier.IpAddress;

    /// <summary>
    /// Roles exentos de rate limiting (ej: Admin, SuperAdmin)
    /// </summary>
    public List<string> ExemptRoles { get; set; } = new();
}

/// <summary>
/// Define cómo se identifica un cliente para rate limiting
/// </summary>
public enum RateLimitIdentifier
{
    /// <summary>
    /// Por dirección IP del cliente
    /// </summary>
    IpAddress,

    /// <summary>
    /// Por UserId (usuario autenticado)
    /// </summary>
    UserId,

    /// <summary>
    /// Por combinación de IP y UserId
    /// </summary>
    IpAndUserId
}

/// <summary>
/// Resultado de evaluación de rate limit
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public int RequestsRemaining { get; set; }
    public int RetryAfterSeconds { get; set; }
    public string? Message { get; set; }
}
