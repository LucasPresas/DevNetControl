using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Infrastructure.Persistence;
using DevNetControl.Api.Domain;
using DevNetControl.Api.Infrastructure.Security;
using BC = BCrypt.Net.BCrypt;
using System.Security.Claims;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-subuser")]
    [Authorize(Policy = "SubResellerOrAbove")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var parentIdClaim = User.FindFirst("UserId")?.Value;
        if (parentIdClaim == null) return Unauthorized();
        
        var parentId = Guid.Parse(parentIdClaim);

        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            return BadRequest("El nombre de usuario ya existe.");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            PasswordHash = BC.HashPassword(request.Password),
            Role = request.Role,
            Credits = 0,
            ParentId = parentId
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Usuario creado con éxito", UserId = newUser.Id });
    }

    [HttpGet("my-subusers")]
    public async Task<IActionResult> GetMySubUsers()
    {
        var parentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var subUsers = await _context.Users
            .Where(u => u.ParentId == parentId)
            .Select(u => new { u.Id, u.UserName, u.Role, u.Credits })
            .ToListAsync();

        return Ok(subUsers);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var user = await _context.Users
            .Include(u => u.Parent)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits,
            Parent = user.Parent == null ? null : new { user.Parent.UserName }
        });
    }

    [HttpGet("me/hierarchy")]
    public async Task<IActionResult> GetMyHierarchy()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        
        var hierarchy = await BuildHierarchyTreeAsync(userId);
        
        return Ok(hierarchy);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (currentRole != "Admin" && user.ParentId != userId)
            return Forbid();

        return Ok(new {
            user.Id,
            user.UserName,
            Role = user.Role.ToString(),
            user.Credits
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateMyUserRequest request)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var currentRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (currentRole != "Admin" && id != userId)
            return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { Message = "Usuario no encontrado" });

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName && u.Id != id))
                return BadRequest("El nombre de usuario ya existe.");
            
            user.UserName = request.UserName;
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BC.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Perfil actualizado correctamente" });
    }

    private async Task<HierarchyNodeDto> BuildHierarchyTreeAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Subordinates)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new KeyNotFoundException("Usuario no encontrado");

        return new HierarchyNodeDto(
            user.Id,
            user.UserName,
            user.Role.ToString(),
            user.Credits,
            user.Subordinates.Select(s => new HierarchyNodeDto(
                s.Id,
                s.UserName,
                s.Role.ToString(),
                s.Credits,
                GetSubordinateChildren(s.Id).Result
            )).ToList()
        );
    }

    private async Task<List<HierarchyNodeDto>> GetSubordinateChildren(Guid parentId)
    {
        var children = await _context.Users
            .Include(u => u.Subordinates)
            .Where(u => u.ParentId == parentId)
            .ToListAsync();

        return children.Select(c => new HierarchyNodeDto(
            c.Id,
            c.UserName,
            c.Role.ToString(),
            c.Credits,
            GetSubordinateChildren(c.Id).Result
        )).ToList();
    }
}

public record CreateUserRequest(string UserName, string Password, UserRole Role);
public record UpdateMyUserRequest(string? UserName = null, string? Password = null);
public record HierarchyNodeDto(Guid Id, string UserName, string Role, decimal Credits, List<HierarchyNodeDto> Children);