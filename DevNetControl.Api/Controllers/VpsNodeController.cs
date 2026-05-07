using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Infrastructure.RateLimiting;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Dtos;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VpsNodeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;
    private readonly SshService _sshService;
    private readonly NodeHealthService _healthService;

    public VpsNodeController(ApplicationDbContext context, EncryptionService encryption, SshService sshService, NodeHealthService healthService)
    {
        _context = context;
        _encryption = encryption;
        _sshService = sshService;
        _healthService = healthService;
    }

    [HttpPost]
    [Authorize(Policy = "ResellerOrAbove")]
    public async Task<IActionResult> CreateNode([FromBody] CreateNodeRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        if (role != "Admin" && role != "SuperAdmin")
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Credits < request.CreditCost)
                return BadRequest(new { Message = "Creditos insuficientes." });

            user.Credits -= request.CreditCost;
        }

        var newNode = new VpsNode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            IP = request.IP,
            SshPort = request.SshPort,
            label = request.Label,
            EncryptedPassword = _encryption.Encrypt(request.Password),
            OwnerId = userId
        };

        _context.VpsNodes.Add(newNode);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Nodo VPS creado exitosamente", NodeId = newNode.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNodes()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var nodes = await _context.VpsNodes
            .Where(n => n.TenantId == tenantId && (role == "Admin" || role == "SuperAdmin" || n.OwnerId == userId))
            .Select(n => new { n.Id, n.IP, n.SshPort, n.label, n.OwnerId })
            .ToListAsync();

        return Ok(nodes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNode(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);
        if (node == null)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        return Ok(new { node.Id, node.IP, node.SshPort, node.label, node.OwnerId });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNode(Guid id, [FromBody] UpdateNodeRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FindAsync(id);
        if (node == null || node.TenantId != tenantId)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        if (!string.IsNullOrEmpty(request.IP))
            node.IP = request.IP;

        if (request.SshPort.HasValue)
            node.SshPort = request.SshPort.Value;

        if (!string.IsNullOrEmpty(request.Label))
            node.label = request.Label;

        if (!string.IsNullOrEmpty(request.Password))
            node.EncryptedPassword = _encryption.Encrypt(request.Password);

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Nodo actualizado correctamente." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNode(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FindAsync(id);
        if (node == null || node.TenantId != tenantId)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        _context.VpsNodes.Remove(node);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Nodo eliminado correctamente." });
    }

    [HttpPost("{id}/test-connection")]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);
        if (node == null)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        var result = await _sshService.TestConnectionAsync(id);

        return result.Connected
            ? Ok(new { Message = result.Message })
            : BadRequest(new { Message = result.Message });
    }

    [HttpPost("{id}/execute")]
    [Authorize(Policy = "ResellerOrAbove")]
    [RateLimit("vps-execute")]
    public async Task<IActionResult> ExecuteCommand(Guid id, [FromBody] ExecuteCommandRequest request)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);
        if (node == null)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        var result = await _sshService.ExecuteCommandAsync(id, request.Command);

        if (!result.Success)
            return BadRequest(new { Message = result.Error });

        return Ok(new { Output = result.Output, Error = result.Error });
    }

    [HttpGet("{id}/metrics")]
    public async Task<IActionResult> GetMetrics(Guid id)
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        var node = await _context.VpsNodes.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantId);
        if (node == null)
            return NotFound(new { Message = "Nodo no encontrado." });

        if (role != "Admin" && role != "SuperAdmin" && node.OwnerId != userId)
            return Forbid();

        var result = await _sshService.GetSystemMetricsAsync(id);

        if (!result.Success)
            return BadRequest(new { Message = result.Error });

        var metrics = ParseMetrics(result.Output);

        return Ok(new { NodeId = id, Metrics = metrics });
    }

    private static Dictionary<string, string> ParseMetrics(string output)
    {
        var metrics = new Dictionary<string, string>();

        foreach (var line in output.Split('\n'))
        {
            var parts = line.Split(':', 2);
            if (parts.Length == 2)
                metrics[parts[0].Trim()] = parts[1].Trim();
        }

        return metrics;
    }

    /// <summary>
    /// Verifica la salud de un nodo específico (manual)
    /// </summary>
    [HttpGet("{id}/health")]
    [Authorize(Policy = "ResellerOrAbove")]
    public async Task<IActionResult> CheckNodeHealth(Guid id)
    {
        var (isOnline, latency, message) = await _healthService.CheckNodeHealthAsync(id);
        return Ok(new
        {
            IsOnline = isOnline,
            LatencyMs = latency,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Obtiene el estado de salud de todos los nodos del tenant
    /// </summary>
    [HttpGet("health")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAllNodesHealth()
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var nodes = await _context.VpsNodes
            .Where(n => n.TenantId == tenantId || n.TenantId == Guid.Empty)
            .ToListAsync();

        var nodeUserCounts = await _context.NodeAccesses
            .Where(na => nodes.Select(n => n.Id).Contains(na.NodeId))
            .GroupBy(na => na.NodeId)
            .Select(g => new { NodeId = g.Key, UserCount = g.Count() })
            .ToListAsync();

        var userCountMap = nodeUserCounts.ToDictionary(x => x.NodeId, x => x.UserCount);

        var result = new List<object>();
        foreach (var node in nodes)
        {
            int userCount = userCountMap.GetValueOrDefault(node.Id, 0);
            object metrics = null;

            if (node.IsOnline)
            {
                try
                {
                    var metricsResult = await _sshService.GetSystemMetricsAsync(node.Id);
                    if (metricsResult.Success)
                    {
                        var parsed = ParseMetrics(metricsResult.Output);
                        metrics = new
                        {
                            CpuPercent = ParseMetricPercent(parsed, "CPU"),
                            RamPercent = ParseMetricPercent(parsed, "Mem"),
                            DiskPercent = ParseMetricPercent(parsed, "Disk"),
                            RamUsed = ParseMetricValue(parsed, "Mem", "MB"),
                            RamTotal = ParseMetricTotal(parsed, "Mem", "MB")
                        };
                    }
                }
                catch { }
            }

            result.Add(new
            {
                node.Id,
                node.IP,
                Label = node.label,
                node.IsOnline,
                node.LastHealthCheck,
                node.LatencyMs,
                UserCount = userCount,
                Metrics = metrics
            });
        }

        return Ok(result);
    }

    private static int ParseMetricPercent(Dictionary<string, string> metrics, string key)
    {
        if (!metrics.TryGetValue(key, out var value)) return 0;
        var percentMatch = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)%");
        return percentMatch.Success ? int.Parse(percentMatch.Groups[1].Value) : 0;
    }

    private static int ParseMetricValue(Dictionary<string, string> metrics, string key, string unit)
    {
        if (!metrics.TryGetValue(key, out var value)) return 0;
        var match = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)\s*" + unit);
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static int ParseMetricTotal(Dictionary<string, string> metrics, string key, string unit)
    {
        if (!metrics.TryGetValue(key, out var value)) return 0;
        var match = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)/(\d+)\s*" + unit);
        return match.Success && match.Groups.Count > 2 ? int.Parse(match.Groups[2].Value) : 0;
    }

    /// <summary>
    /// Eliminación masiva de nodos
    /// </summary>
    [HttpPost("bulk-delete")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> BulkDeleteNodes([FromBody] BulkDeleteNodesRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        if (role != "Admin" && role != "SuperAdmin")
            return Forbid();

        int successCount = 0;
        int failCount = 0;
        var errors = new List<string>();

        foreach (var id in request.NodeIds)
        {
            var node = await _context.VpsNodes.FindAsync(id);
            if (node == null || node.TenantId != tenantId)
            {
                failCount++;
                errors.Add($"Nodo {id} no encontrado");
                continue;
            }

            _context.VpsNodes.Remove(node);
            successCount++;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Proceso completado: {successCount} eliminados, {failCount} fallidos.",
            SuccessCount = successCount,
            FailCount = failCount,
            Errors = errors
        });
    }
}

public record CreateNodeRequest(string IP, int SshPort, string Label, string Password, decimal CreditCost = 0);
public record UpdateNodeRequest(string? IP = null, int? SshPort = null, string? Label = null, string? Password = null);
public record ExecuteCommandRequest(string Command);
