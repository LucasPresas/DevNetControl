using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Infrastructure.Services;
using DevNetControl.Api.Domain;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly CreditService _creditService;

    public AdminController(ApplicationDbContext context, CreditService creditService)
    {
        _context = context;
        _creditService = creditService;
    }

    [HttpGet("dashboard-data")]
    public IActionResult GetSensitiveData()
    {
        return Ok(new {
            Message = "Bienvenido, Admin. Estás viendo datos que solo el Admin puede ver.",
            ServerStatus = "All nodes operational",
            TotalRevenue = 1500.50
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users
            .Include(u => u.OwnedNodes)
            .Select(u => new {
                u.Id,
                u.UserName,
                Role = u.Role.ToString(),
                u.Credits,
                ParentId = u.ParentId,
                SubordinatesCount = u.Subordinates.Count,
                NodesCount = u.OwnedNodes.Count
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserDetail(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Parent)
            .Include(u => u.Subordinates)
            .Include(u => u.OwnedNodes)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            Parent = user.Parent == null ? null : new { user.Parent.Id, user.Parent.UserName },
            Subordinates = user.Subordinates.Select(s => new { s.Id, s.UserName, s.Role }).ToList(),
            Nodes = user.OwnedNodes.Select(n => new { n.Id, n.IP, n.SshPort, n.label }).ToList()
        });
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (request.Role.HasValue)
            user.Role = request.Role.Value;

        if (request.Credits.HasValue && request.Credits.Value >= 0)
            user.Credits = request.Credits.Value;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario actualizado correctamente" });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (user.Role == UserRole.Admin)
            return BadRequest(new { Message = "No se puede eliminar el administrador principal" });

        if (user.Subordinates.Any())
            return BadRequest(new { Message = "El usuario tiene subordinados. Reasignálos primero." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario eliminado correctamente" });
    }

    [HttpPost("users/{id}/add-credits")]
    public async Task<IActionResult> AddCredits(Guid id, [FromBody] AddCreditsRequest request)
    {
        var result = await _creditService.AddCreditsAsync(id, request.Amount);
        
        if (!result.Success)
            return BadRequest(new { Message = result.Message });

        return Ok(new { Message = result.Message });
    }
}

public record UpdateUserRequest(UserRole? Role = null, decimal? Credits = null);
public record AddCreditsRequest(decimal Amount);