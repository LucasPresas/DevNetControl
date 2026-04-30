using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Renci.SshNet;

namespace DevNetControl.Api.Infrastructure.Services;

public class SshService
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;

    public SshService(ApplicationDbContext context, EncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<(bool Connected, string Message)> TestConnectionAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, "Nodo no encontrado.");

        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);

            using var client = new SshClient(node.IP, node.SshPort, "root", password);
            client.Connect();

            var connected = client.IsConnected;
            client.Disconnect();

            return connected
                ? (true, $"Conexión exitosa a {node.IP}:{node.SshPort}")
                : (false, "No se pudo establecer conexión.");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(Guid nodeId, string command)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);

            using var client = new SshClient(node.IP, node.SshPort, "root", password);
            client.Connect();

            if (!client.IsConnected)
                return (false, string.Empty, "No se pudo conectar al nodo.");

            var cmd = client.CreateCommand(command);
            var result = cmd.Execute();

            client.Disconnect();

            return (true, result, cmd.Error);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Error ejecutando comando: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Output, string Error)> GetSystemMetricsAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null)
            return (false, string.Empty, "Nodo no encontrado.");

        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);

            using var client = new SshClient(node.IP, node.SshPort, "root", password);
            client.Connect();

            if (!client.IsConnected)
                return (false, string.Empty, "No se pudo conectar al nodo.");

            var cpuCmd = client.CreateCommand("top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | head -1");
            var cpuResult = cpuCmd.Execute().Trim();

            var memCmd = client.CreateCommand("free -m | awk 'NR==2{printf \"%.1f/%.1f MB (%.1f%%)\", $3,$2,$3*100/$2 }'");
            var memResult = memCmd.Execute().Trim();

            var diskCmd = client.CreateCommand("df -h / | awk 'NR==2{printf \"%s/%s (%s)\", $3,$2,$5}'");
            var diskResult = diskCmd.Execute().Trim();

            var uptimeCmd = client.CreateCommand("uptime -p");
            var uptimeResult = uptimeCmd.Execute().Trim();

            client.Disconnect();

            var output = $"CPU: {cpuResult}%\nMemoria: {memResult}\nDisco: {diskResult}\nUptime: {uptimeResult}";

            return (true, output, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Error obteniendo métricas: {ex.Message}");
        }
    }
}
