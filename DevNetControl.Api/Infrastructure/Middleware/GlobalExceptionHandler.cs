using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace DevNetControl.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware global para manejar excepciones no capturadas en la aplicación.
/// Proporciona respuestas consistentes en formato JSON para todos los tipos de error.
/// </summary>
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
            _logger.LogError(ex, "Error no controlado: {Message} | StackTrace: {StackTrace}", 
                ex.Message, ex.StackTrace);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ErrorResponse
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "Ocurrió un error interno en el servidor.",
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        // Manejo de excepciones específicas
        if (exception is ValidationException validationEx)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = "Error de validación.";
            response.Errors = validationEx.Errors
                .Select(e => new FieldError(e.PropertyName, e.ErrorMessage))
                .ToList();
        }
        else if (exception is KeyNotFoundException knfEx)
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.Message = knfEx.Message;
        }
        else if (exception is UnauthorizedAccessException uaEx)
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.Message = "No autorizado. Verifique sus credenciales.";
        }
        else if (exception is ArgumentNullException aneEx)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = $"Parámetro requerido no proporcionado: {aneEx.ParamName}";
        }
        else if (exception is ArgumentException argEx)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = $"Argumento inválido: {argEx.Message}";
        }
        else if (exception is DbUpdateException dbEx)
        {
            response.StatusCode = (int)HttpStatusCode.Conflict;
            response.Message = "Error al actualizar la base de datos. Verifique los datos ingresados.";
            if (dbEx.InnerException?.Message.Contains("UNIQUE constraint failed") ?? false)
                response.Message = "El registro ya existe en la base de datos.";
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            response.StatusCode = (int)HttpStatusCode.Conflict;
            response.Message = "Error de concurrencia: El registro fue modificado por otro usuario.";
        }
        else if (exception is OperationCanceledException)
        {
            response.StatusCode = (int)HttpStatusCode.RequestTimeout;
            response.Message = "La operación fue cancelada o excedió el tiempo de espera.";
        }
        else if (exception is InvalidOperationException ioEx)
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Message = ioEx.Message;
        }
        else
        {
            // Error genérico - no exponer detalles en producción
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Message = "Ocurrió un error inesperado en el servidor.";
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// Respuesta estándar para errores HTTP
/// </summary>
public record ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? TraceId { get; set; }
    public List<FieldError>? Errors { get; set; }
}

/// <summary>
/// Error de validación a nivel de campo
/// </summary>
public record FieldError(string Field, string Message);

/// <summary>
/// Extensiones para registrar el middleware en la tubería de aplicación
/// </summary>
public static class GlobalExceptionHandlerExtensions
{
    /// <summary>
    /// Registra el middleware global de manejo de excepciones en la tubería de aplicación
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandler>();
    }
}
