using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class NodeAccessController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public NodeAccessController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserNodeAccess(Guid userId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var access = await _context.NodeAccesses
            .Include(na => na.Node)
            .Where(na => na.UserId == userId)
            .Select(na => new
            {
                na.NodeId,
                na.Node.IP,
                na.Node.SshPort,
                na.Node.label,
                na.Node.OwnerId
            })
            .ToListAsync();

        return Ok(access);
    }

    [HttpPost]
    public async Task<IActionResult> GrantNodeAccess([FromBody] GrantNodeAccessRequest request)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null || user.TenantId != tenantId)
            return NotFound(new { Message = "Usuario no encontrado." });

        var node = await _context.VpsNodes.FindAsync(request.NodeId);
        if (node == null || node.TenantId != tenantId)
            return NotFound(new { Message = "Nodo no encontrado." });

        var existing = await _context.NodeAccesses
            .AnyAsync(na => na.UserId == request.UserId && na.NodeId == request.NodeId);

        if (existing)
            return BadRequest(new { Message = "El usuario ya tiene acceso a este nodo." });

        var access = new NodeAccess
        {
            Id = Guid.NewGuid(),
            NodeId = request.NodeId,
            UserId = request.UserId
        };

        _context.NodeAccesses.Add(access);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Acceso al nodo concedido." });
    }

    [HttpDelete("user/{userId}/node/{nodeId}")]
    public async Task<IActionResult> RevokeNodeAccess(Guid userId, Guid nodeId)
    {
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);

        var access = await _context.NodeAccesses
            .Include(na => na.Node)
            .FirstOrDefaultAsync(na => na.UserId == userId && na.NodeId == nodeId);

        if (access == null || access.Node.TenantId != tenantId)
            return NotFound(new { Message = "Acceso no encontrado." });

        _context.NodeAccesses.Remove(access);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Acceso al nodo revocado." });
    }

    [HttpGet("my-nodes")]
    public async Task<IActionResult> GetMyAvailableNodes()
    {
        var userId = ClaimsHelper.GetCurrentUserId(User);
        var tenantId = ClaimsHelper.GetCurrentTenantId(User);
        var role = ClaimsHelper.GetCurrentRole(User);

        if (role == "Admin" || role == "SuperAdmin")
        {
            var allNodes = await _context.VpsNodes
                .Where(n => n.TenantId == tenantId)
                .Select(n => new { n.Id, n.IP, n.SshPort, n.label, n.OwnerId })
                .ToListAsync();

            return Ok(allNodes);
        }

        var myAccess = await _context.NodeAccesses
            .Include(na => na.Node)
            .Where(na => na.UserId == userId)
            .Select(na => new
            {
                na.Node.Id,
                na.Node.IP,
                na.Node.SshPort,
                na.Node.label,
                na.Node.OwnerId
            })
            .ToListAsync();

        var myOwnedNodes = await _context.VpsNodes
            .Where(n => n.TenantId == tenantId && n.OwnerId == userId)
            .Select(n => new { n.Id, n.IP, n.SshPort, n.label, n.OwnerId })
            .ToListAsync();

        var combined = myAccess.Union(myOwnedNodes).DistinctBy(n => n.Id).ToList();

        return Ok(combined);
    }
}

public record GrantNodeAccessRequest(Guid UserId, Guid NodeId);
