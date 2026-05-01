using System.Security.Claims;

namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Servicio central de rate limiting. Evalúa si un request debe ser permitido.
/// Agnóstico al almacenamiento (usa IRateLimitStore).
/// </summary>
public class RateLimitService
{
    private readonly IRateLimitStore _store;
    private readonly ILogger<RateLimitService> _logger;
    private readonly Dictionary<string, RateLimitPolicy> _policies = new();

    public RateLimitService(IRateLimitStore store, ILogger<RateLimitService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Registra una política de rate limiting
    /// </summary>
    public void RegisterPolicy(RateLimitPolicy policy)
    {
        if (string.IsNullOrEmpty(policy.Key))
            throw new ArgumentException("Policy key cannot be null or empty");

        _policies[policy.Key] = policy;
        _logger.LogInformation("Rate limit policy registered: {Key} ({MaxRequests} requests per {WindowSeconds}s)", 
            policy.Key, policy.MaxRequests, policy.WindowSeconds);
    }

    /// <summary>
    /// Evalúa si un request debe ser permitido según la política
    /// </summary>
    public async Task<RateLimitResult> EvaluateAsync(
        string policyKey, 
        HttpContext context,
        ClaimsPrincipal? user = null)
    {
        if (!_policies.TryGetValue(policyKey, out var policy))
        {
            _logger.LogWarning("Rate limit policy not found: {Key}", policyKey);
            return new RateLimitResult { IsAllowed = true };
        }

        // Verificar si el usuario está exento por rol
        if (user != null && IsUserExempt(user, policy))
        {
            return new RateLimitResult { IsAllowed = true };
        }

        // Generar clave de identidad
        var identityKey = GenerateIdentityKey(policyKey, context, user, policy.Identifier);

        // Obtener contador actual
        var count = await _store.IncrementAsync(identityKey, policy.WindowSeconds);
        var ttl = await _store.GetTtlAsync(identityKey) ?? policy.WindowSeconds;

        var isAllowed = count <= policy.MaxRequests;

        if (!isAllowed)
        {
            _logger.LogWarning(
                "Rate limit exceeded: Policy={Key}, Identity={Identity}, Requests={Count}/{Max}, RetryAfter={Ttl}s",
                policyKey, identityKey, count, policy.MaxRequests, ttl);
        }

        return new RateLimitResult
        {
            IsAllowed = isAllowed,
            RequestsRemaining = Math.Max(0, policy.MaxRequests - count),
            RetryAfterSeconds = ttl,
            Message = policy.ErrorMessage ?? 
                      $"Rate limit exceeded. Maximum {policy.MaxRequests} requests per {policy.WindowSeconds} seconds allowed."
        };
    }

    /// <summary>
    /// Limpia entradas expiradas del almacén
    /// </summary>
    public async Task<int> CleanupAsync()
    {
        return await _store.CleanupAsync();
    }

    /// <summary>
    /// Verifica si un usuario está exento de rate limiting
    /// </summary>
    private bool IsUserExempt(ClaimsPrincipal user, RateLimitPolicy policy)
    {
        if (policy.ExemptRoles.Count == 0)
            return false;

        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(userRole))
            return false;

        return policy.ExemptRoles.Contains(userRole);
    }

    /// <summary>
    /// Genera la clave de identidad para el almacén según el tipo de identificador
    /// </summary>
    private string GenerateIdentityKey(
        string policyKey,
        HttpContext context,
        ClaimsPrincipal? user,
        RateLimitIdentifier identifier)
    {
        var ipAddress = GetClientIpAddress(context);
        var userId = user?.FindFirst("UserId")?.Value;

        return identifier switch
        {
            RateLimitIdentifier.IpAddress =>
                $"rl:{policyKey}:ip:{ipAddress}",

            RateLimitIdentifier.UserId when !string.IsNullOrEmpty(userId) =>
                $"rl:{policyKey}:uid:{userId}",

            RateLimitIdentifier.IpAndUserId =>
                $"rl:{policyKey}:ip:{ipAddress}:uid:{userId ?? "anon"}",

            _ => $"rl:{policyKey}:ip:{ipAddress}"
        };
    }

    /// <summary>
    /// Obtiene la dirección IP del cliente, considerando proxies
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Verificar encabezados de proxy comunes
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').First()?.Trim();
        
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = context.Connection.RemoteIpAddress?.ToString();

        return ipAddress ?? "unknown";
    }
}
