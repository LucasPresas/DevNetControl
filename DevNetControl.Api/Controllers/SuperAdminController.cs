using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/superadmin")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuperAdminController(ApplicationDbContext context) => _context = context;

    [HttpPost("provision-node")]
    public async Task<IActionResult> ProvisionNode([FromBody] ProvisionNodeRequest req)
    {
        // REFACTOR: Usando los nombres exactos de tu entidad VpsNode
        var node = new VpsNode
        {
            Id = Guid.NewGuid(),
            TenantId = req.TargetTenantId, 
            IP = req.IpAddress,      // Según tu controlador
            SshPort = req.SshPort,   // Según tu controlador
            label = req.NodeName,    // En tu entidad se llama 'label'
            // Como SuperAdmin, el nodo no tiene un 'OwnerId' personal necesariamente, 
            // o puedes asignarte a ti mismo si el modelo lo requiere.
        };

        _context.VpsNodes.Add(node);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Nodo asignado al cliente correctamente", NodeId = node.Id });
    }
}

// Actualizamos el Record para incluir el puerto SSH
public record ProvisionNodeRequest(Guid TargetTenantId, string NodeName, string IpAddress, int SshPort = 22);