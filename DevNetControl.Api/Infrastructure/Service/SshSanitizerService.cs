using System.Text.RegularExpressions;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio dedicado a sanitizar y validar comandos SSH para prevenir inyección.
/// </summary>
public class SshSanitizerService
{
    // Caracteres y patrones peligrosos para inyección de comandos
    private static readonly string[] DangerousPatterns =
    {
        @"[;&|`$]",           // Separadores y ejecución de comandos
        @"\$\(",               // Substituciones de shell
        @"\|\|",              // OR lógico
        @"&&\b",              // AND lógico  
        @">>",                // Redirección de salida
        @"<<\s*EOF",          // Here-documents
        @"rm\s+-rf\s+/",     // rm recursivo en raíz
        @"wget\s+.*\|",       // wget piped
        @"curl\s+.*\|"        // curl piped
    };

    /// <summary>
    /// Validación estricta: lanza excepción si el comando es inseguro.
    /// </summary>
    public void ValidateStrict(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Comando no puede estar vacío.");

        // Longitud máxima para prevenir buffer overflow
        if (command.Length > 500)
            throw new ArgumentException("Comando demasiado largo.");

        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(command, pattern))
                throw new ArgumentException($"Comando contiene patrones no permitidos: {pattern}");
        }
    }

    /// <summary>
    /// Sanitización básica: remueve caracteres peligrosos (menos restrictiva).
    /// </summary>
    public string Sanitize(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return string.Empty;

        // Remover caracteres de control y secuencias de escape
        var sanitized = Regex.Replace(command, @"[\x00-\x1F\x7F]", "");
        
        // Escapar comillas si es necesario para strings
        sanitized = sanitized.Replace("\"", "\\\"");
        
        return sanitized.Trim();
    }
}
