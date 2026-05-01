namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Atributo para aplicar rate limiting a un endpoint.
/// Uso: [RateLimit("auth-login")]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RateLimitAttribute : Attribute
{
    /// <summary>
    /// Clave de la política de rate limiting a aplicar
    /// </summary>
    public string PolicyKey { get; set; }

    /// <summary>
    /// Número máximo de requests (si no se especifica, usa la política registrada)
    /// </summary>
    public int? MaxRequests { get; set; }

    /// <summary>
    /// Ventana de tiempo en segundos (si no se especifica, usa la política registrada)
    /// </summary>
    public int? WindowSeconds { get; set; }

    public RateLimitAttribute(string policyKey)
    {
        if (string.IsNullOrWhiteSpace(policyKey))
            throw new ArgumentException("Policy key cannot be null or empty", nameof(policyKey));

        PolicyKey = policyKey;
    }
}
