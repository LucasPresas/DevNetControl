using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Renci.SshNet;
using System.Text.RegularExpressions;

namespace DevNetControl.Api.Infrastructure.Services;

    public class SshService
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _encryption;
        private readonly SshSanitizerService _sanitizer;

        public SshService(ApplicationDbContext context, EncryptionService encryption, SshSanitizerService sanitizer)
        {
            _context = context;
            _encryption = encryption;
            _sanitizer = sanitizer;
        }

    // FIX CS1061: GetActiveSessionsAsync para el MonitorController
    public async Task<(int Count, List<int> Pids)> GetActiveSessionsAsync(Guid nodeId, string username)
    {
        var command = $"ps -u {username} -o pid,comm | grep sshd | awk '{{print $1}}'";
        var result = await ExecuteCommandAsync(nodeId, command);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            return (0, new List<int>());

        var pids = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => int.TryParse(p, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        return (pids.Count, pids);
    }

    // FIX CS1061: EnforcementKickAsync para el MonitorController
    public async Task<bool> EnforcementKickAsync(Guid nodeId, string username, int maxAllowed)
    {
        var (count, pids) = await GetActiveSessionsAsync(nodeId, username);
        
        if (count > maxAllowed && pids.Count > 0)
        {
            // Matamos el proceso más antiguo para liberar cupo
            await ExecuteCommandAsync(nodeId, $"kill -9 {pids.First()}");
            return true;
        }
        return false;
    }

    // FIX CS1061: TestConnectionAsync para VpsNodeController
    public async Task<(bool Connected, string Message)> TestConnectionAsync(Guid nodeId)
    {
        var (client, error) = await GetConnectedClientAsync(nodeId);
        if (client == null) return (false, error ?? "Error de conexión.");

        using (client)
        {
            var msg = $"Conexión exitosa a {client.ConnectionInfo.Host}";
            await Task.Run(() => client.Disconnect());
            return (true, msg);
        }
    }

    // FIX CS1061: GetSystemMetricsAsync para VpsNodeController
    public async Task<(bool Success, string Output, string Error)> GetSystemMetricsAsync(Guid nodeId)
    {
        const string metricsScript = @"
            CPU=$(top -bn1 | grep 'Cpu(s)' | awk '{print $2}' | head -1)
            MEM=$(free -m | awk 'NR==2{printf ""%.1f/%.1f MB (%.1f%%)"", $3,$2,$3*100/$2 }')
            DISK=$(df -h / | awk 'NR==2{printf ""%s/%s (%s)"", $3,$2,$5}')
            echo ""CPU: $CPU% | Mem: $MEM | Disk: $DISK""";

        return await ExecuteCommandAsync(nodeId, metricsScript);
    }

    public async Task<(bool Success, string Output, string Error)> ExecuteCommandAsync(Guid nodeId, string command)
    {
        // Sanitización de entrada
        try 
        {
            _sanitizer.ValidateStrict(command);
        }
        catch (ArgumentException ex)
        {
            return (false, "", ex.Message);
        }

        var (client, error) = await GetConnectedClientAsync(nodeId);
        if (client == null) return (false, "", error ?? "Error de conexión.");

        using (client)
        {
            try
            {
                var cmd = client.CreateCommand(command);
                var result = await Task.Run(() => cmd.Execute());
                return (cmd.ExitStatus == 0, result, cmd.Error);
            }
            catch (Exception ex) { return (false, "", ex.Message); }
        }
    }

    private async Task<(SshClient? Client, string? ErrorMessage)> GetConnectedClientAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null) return (null, "Nodo no encontrado.");

        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);
            var client = new SshClient(node.IP, node.SshPort, "root", password);
            await Task.Run(() => client.Connect());
            return client.IsConnected ? (client, null) : (null, "No se pudo conectar.");
        }
        catch (Exception ex) { return (null, ex.Message); }
    }
}