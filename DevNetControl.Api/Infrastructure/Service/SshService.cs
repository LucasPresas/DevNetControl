using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Text.RegularExpressions;

namespace DevNetControl.Api.Infrastructure.Services;

// REFACTOR: El servicio ahora es más robusto, eficiente y seguro.
// 1. Centraliza la lógica de conexión en un método privado.
// 2. Usa Task.Run para no bloquear los hilos del servidor con operaciones síncronas de red.
// 3. Optimiza la obtención de métricas para usar una sola conexión SSH.
// 4. Añade una capa básica de seguridad contra inyección de comandos.
public class SshService
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;

    public SshService(ApplicationDbContext context, EncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    private async Task<(SshClient? Client, string? ErrorMessage)> GetConnectedClientAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
        {
            return (null, "Nodo no encontrado.");
        }

        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);
            var client = new SshClient(node.IP, node.SshPort, "root", password);

            // Envolvemos la llamada bloqueante en Task.Run para no agotar los hilos de ASP.NET
            await Task.Run(() => client.Connect());

            if (!client.IsConnected)
            {
                return (null, "No se pudo establecer conexión con el nodo.");
            }

            return (client, null);
        }
        catch (SshAuthenticationException ex)
        {
            return (null, $"Error de autenticación: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Error de conexión: {ex.Message}");
        }
    }

    public async Task<(bool Connected, string Message)> TestConnectionAsync(Guid nodeId)
    {
        var (client, errorMessage) = await GetConnectedClientAsync(nodeId);

        if (client == null)
        {
            return (false, errorMessage ?? "Error desconocido.");
        }

        using (client)
        {
            var message = $"Conexión exitosa a {client.ConnectionInfo.Host}:{client.ConnectionInfo.Port}";
            client.Disconnect();
            return (true, message);
        }
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(Guid nodeId, string command)
    {
        // REGLA DE ORO #3: SANITIZACIÓN DE COMANDOS
        // Esta es una mitigación básica. La estrategia ideal es no permitir comandos de texto libre.
        // En su lugar, la API debería recibir un "nombre de comando" y parámetros, y este servicio
        // construiría el comando real a partir de una plantilla segura.
        if (Regex.IsMatch(command, @"[;`&|]"))
        {
            return (false, string.Empty, "El comando contiene caracteres no permitidos por seguridad.");
        }

        var (client, errorMessage) = await GetConnectedClientAsync(nodeId);
        if (client == null)
        {
            return (false, string.Empty, errorMessage ?? "Error desconocido.");
        }

        using (client)
        {
            try
            {
                var cmd = client.CreateCommand(command);
                var result = await Task.Run(() => cmd.Execute());
                return (cmd.ExitStatus == 0, result, cmd.Error);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error ejecutando comando: {ex.Message}");
            }
        }
    }


    public async Task<(bool Success, string Output, string Error)> GetSystemMetricsAsync(Guid nodeId)
    {
        var (client, errorMessage) = await GetConnectedClientAsync(nodeId);
        if (client == null)
        {
            return (false, string.Empty, errorMessage ?? "Error desconocido.");
        }

        using (client)
        {
            try
            {
                // Optimizacion: Ejecutamos todos los comandos en una sola sesión SSH.
                const string metricsScript = @"
                    CPU=$(top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | head -1)
                    MEM=$(free -m | awk 'NR==2{printf ""%.1f/%.1f MB (%.1f%%)"", $3,$2,$3*100/$2 }')
                    DISK=$(df -h / | awk 'NR==2{printf ""%s/%s (%s)"", $3,$2,$5}')
                    UPTIME=$(uptime -p)
                    echo ""CPU: $CPU%""
                    echo ""Memoria: $MEM""
                    echo ""Disco: $DISK""
                    echo ""Uptime: $UPTIME""
                ";

                var cmd = client.CreateCommand(metricsScript);
                var output = await Task.Run(() => cmd.Execute());

                if (cmd.ExitStatus != 0)
                {
                    return (false, string.Empty, cmd.Error);
                }
                
                return (true, output.Trim(), string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Error obteniendo métricas: {ex.Message}");
            }
        }
    }
}
