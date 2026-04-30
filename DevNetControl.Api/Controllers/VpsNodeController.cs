using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Infrastructure.Security;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VpsNodeController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly EncryptionService _encryption;
    private readonly SshService _sshService;

    public VpsNodeController(ApplicationDbContext context, EncryptionService encryption, SshService sshService)
    {
        _context = context;
        _encryption = encryption;
        _sshService = sshService;
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
}

public record CreateNodeRequest(string IP, int SshPort, string Label, string Password, decimal CreditCost = 0);
public record UpdateNodeRequest(string? IP = null, int? SshPort = null, string? Label = null, string? Password = null);
public record ExecuteCommandRequest(string Command);
