using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DevNetControl.Api.Infrastructure.Services;

/// <summary>
/// Servicio para monitoreo automático de disponibilidad de nodos VPS.
/// </summary>
public class NodeHealthService
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;
    private readonly ILogger<NodeHealthService> _logger;

    public NodeHealthService(
        ApplicationDbContext context, 
        EncryptionService encryption, 
        ILogger<NodeHealthService> logger)
    {
        _context = context;
        _encryption = encryption;
        _logger = logger;
    }

    /// <summary>
    /// Verifica la salud de un nodo específico vía SSH.
    /// </summary>
    public async Task<(bool IsOnline, long LatencyMs, string Message)> CheckNodeHealthAsync(Guid nodeId)
    {
        var node = await _context.VpsNodes.FindAsync(nodeId);
        if (node == null) return (false, 0, "Nodo no encontrado.");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var password = _encryption.Decrypt(node.EncryptedPassword);
            using var client = new Renci.SshNet.SshClient(node.IP, node.SshPort, "root", password);
            
            await Task.Run(() => client.Connect());
            var isConnected = client.IsConnected;
            client.Disconnect();

            stopwatch.Stop();
            var latency = stopwatch.ElapsedMilliseconds;

            // Actualizar entidad
            node.LastHealthCheck = DateTime.UtcNow;
            node.IsOnline = isConnected;
            node.LatencyMs = latency;
            await _context.SaveChangesAsync();

            return isConnected 
                ? (true, latency, $"Nodo en línea ({latency}ms)")
                : (false, latency, "No se pudo conectar al nodo.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Error verificando salud del nodo {NodeId}", nodeId);
            
            node.LastHealthCheck = DateTime.UtcNow;
            node.IsOnline = false;
            node.LatencyMs = stopwatch.ElapsedMilliseconds;
            await _context.SaveChangesAsync();

            return (false, stopwatch.ElapsedMilliseconds, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica la salud de todos los nodos de un tenant.
    /// </summary>
    public async Task CheckAllNodesAsync(Guid? tenantId = null)
    {
        var query = _context.VpsNodes.AsQueryable();
        if (tenantId.HasValue)
            query = query.Where(n => n.TenantId == tenantId.Value || n.TenantId == Guid.Empty);

        var nodes = await query.ToListAsync();
        foreach (var node in nodes)
        {
            await CheckNodeHealthAsync(node.Id);
        }
    }
}
