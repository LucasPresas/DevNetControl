using System.Net;

namespace DevNetControl.Api.Infrastructure.RateLimiting;

/// <summary>
/// Middleware que aplica rate limiting basado en políticas registradas.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitService _rateLimitService;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(RequestDelegate next, RateLimitService rateLimitService, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var rateLimitAttribute = endpoint?.Metadata.GetOrderedMetadata<RateLimitAttribute>().FirstOrDefault();

        if (rateLimitAttribute != null)
        {
            var result = await _rateLimitService.EvaluateAsync(
                rateLimitAttribute.PolicyKey,
                context,
                context.User);

            // Agregar headers informativos (usando Append para evitar excepciones de clave duplicada)
            context.Response.Headers.Append("X-RateLimit-Limit", rateLimitAttribute.MaxRequests?.ToString() ?? "N/A");
            context.Response.Headers.Append("X-RateLimit-Remaining", result.RequestsRemaining.ToString());
            
            if (!result.IsAllowed)
            {
                context.Response.Headers.Append("X-RateLimit-Reset", ((int)DateTimeOffset.UtcNow.AddSeconds(result.RetryAfterSeconds).ToUnixTimeSeconds()).ToString());
                context.Response.Headers.Append("Retry-After", result.RetryAfterSeconds.ToString());

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";

                var response = new
                {
                    StatusCode = (int)HttpStatusCode.TooManyRequests,
                    Message = result.Message,
                    RetryAfterSeconds = result.RetryAfterSeconds,
                    Timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsJsonAsync(response);
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extensión para registrar el middleware en la tubería
/// </summary>
public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}
