using System.Net;
using System.Text.Json;

namespace DevNetControl.Api.Infrastructure.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "Ocurrió un error interno en el servidor.",
            Timestamp = DateTime.UtcNow
        };

        if (exception is KeyNotFoundException)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.Message = exception.Message;
        }
        else if (exception is UnauthorizedAccessException)
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.Message = "No autorizado. Verifique sus credenciales.";
        }
        else if (exception is FluentValidation.ValidationException validationEx)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = "Error de validación.";
            response.Errors = validationEx.Errors.Select(e => new FieldError(e.PropertyName, e.ErrorMessage)).ToList();
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<FieldError>? Errors { get; set; }
}

public record FieldError(string Field, string Message);

public static class GlobalExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandler>();
    }
}
