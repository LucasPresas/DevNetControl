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
[Authorize] // Todos los endpoints aquí requieren token
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public UserController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost("create-subuser")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Obtenemos el ID del usuario que está logueado (el padre)
        var parentIdClaim = User.FindFirst("UserId")?.Value;
        if (parentIdClaim == null) return Unauthorized();
        
        var parentId = Guid.Parse(parentIdClaim);

        // Verificamos si el nombre de usuario ya existe
        if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            return BadRequest("El nombre de usuario ya existe.");

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            PasswordHash = BC.HashPassword(request.Password),
            Role = request.Role,
            Credits = 0,
            ParentId = parentId // Lo vinculamos al que lo está creando
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
}

public record CreateUserRequest(string UserName, string Password, UserRole Role);