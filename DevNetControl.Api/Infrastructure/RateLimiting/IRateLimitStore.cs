namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Interface agnóstica para almacenamiento de datos de rate limiting.
/// Puede implementarse con in-memory, Redis, base de datos, etc.
/// </summary>
public interface IRateLimitStore
{
    /// <summary>
    /// Incrementa el contador de requests para una clave
    /// </summary>
    /// <returns>Número total de requests en la ventana actual</returns>
    Task<int> IncrementAsync(string key, int windowSeconds);

    /// <summary>
    /// Obtiene el contador de requests para una clave
    /// </summary>
    Task<int> GetCountAsync(string key);

    /// <summary>
    /// Obtiene el TTL (tiempo de expiración) de una clave en segundos
    /// </summary>
    Task<int?> GetTtlAsync(string key);

    /// <summary>
    /// Limpia todas las claves expiradas
    /// </summary>
    Task<int> CleanupAsync();
}
