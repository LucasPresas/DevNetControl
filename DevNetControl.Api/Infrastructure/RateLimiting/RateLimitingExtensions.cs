namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Extensiones para registrar y configurar rate limiting en la inyección de dependencias
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Registra el servicio de rate limiting en el contenedor DI
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddSingleton<IRateLimitStore, InMemoryRateLimitStore>();
        services.AddSingleton<RateLimitService>();
        return services;
    }

    /// <summary>
    /// Configura las políticas de rate limiting por defecto para endpoints sensibles
    /// </summary>
    public static RateLimitService ConfigureDefaultPolicies(this RateLimitService service)
    {
        // Rate limiting para login - muy restrictivo
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "auth-login",
            MaxRequests = 5,
            WindowSeconds = 60,
            Identifier = RateLimitIdentifier.IpAddress,
            ErrorMessage = "Demasiados intentos de login. Intenta de nuevo en un minuto.",
            ExemptRoles = new() // No exentos
        });

        // Rate limiting para cambio de contraseña
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "auth-change-password",
            MaxRequests = 3,
            WindowSeconds = 300, // 5 minutos
            Identifier = RateLimitIdentifier.UserId,
            ErrorMessage = "Demasiados intentos de cambio de contraseña. Intenta más tarde.",
            ExemptRoles = new() // No exentos
        });

        // Rate limiting para creación de usuarios
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "user-create",
            MaxRequests = 10,
            WindowSeconds = 60,
            Identifier = RateLimitIdentifier.UserId,
            ErrorMessage = "Límite de creación de usuarios alcanzado.",
            ExemptRoles = new() { "Admin", "SuperAdmin" } // Admins exentos
        });

        // Rate limiting para transferencia de créditos
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "credit-transfer",
            MaxRequests = 20,
            WindowSeconds = 60,
            Identifier = RateLimitIdentifier.UserId,
            ErrorMessage = "Límite de transferencias alcanzado.",
            ExemptRoles = new() { "SuperAdmin" }
        });

        // Rate limiting para ejecución de comandos SSH
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "vps-execute",
            MaxRequests = 15,
            WindowSeconds = 60,
            Identifier = RateLimitIdentifier.IpAndUserId,
            ErrorMessage = "Demasiados comandos. Espera un poco.",
            ExemptRoles = new() { "Admin", "SuperAdmin" }
        });

        // Rate limiting general para API
        service.RegisterPolicy(new RateLimitPolicy
        {
            Key = "api-general",
            MaxRequests = 100,
            WindowSeconds = 60,
            Identifier = RateLimitIdentifier.IpAddress,
            ErrorMessage = "Límite de requests alcanzado.",
            ExemptRoles = new() { "Admin", "SuperAdmin" }
        });

        return service;
    }

    /// <summary>
    /// Tarea de limpieza periódica de entradas expiradas
    /// </summary>
    public static IApplicationBuilder UseRateLimitCleanup(this IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetRequiredService<RateLimitService>();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5)); // Cada 5 minutos
                    await service.CleanupAsync();
                }
                catch (Exception ex)
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<RateLimitService>>();
                    logger.LogError(ex, "Error during rate limit cleanup");
                }
            }
        });

        return app;
    }
}
