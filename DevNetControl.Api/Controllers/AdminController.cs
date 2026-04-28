using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevNetControl.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // <--- ESTO ES LA LLAVE: Solo entra el rol Admin
public class AdminController : ControllerBase
{
    [HttpGet("dashboard-data")]
    public IActionResult GetSensitiveData()
    {
        return Ok(new {
            Message = "Bienvenido, Lucas. Estás viendo datos que solo el Admin puede ver.",
            ServerStatus = "All nodes operational",
            TotalRevenue = 1500.50
        });
    }
}