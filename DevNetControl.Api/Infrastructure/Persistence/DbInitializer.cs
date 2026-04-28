using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;
using BC = BCrypt.Net.BCrypt; // Alias para que sea más fácil de usar

// 1. VERIFICÁ QUE EL NAMESPACE SEA ESTE:
namespace DevNetControl.Api.Infrastructure.Persistence
{
    // 2. VERIFICÁ QUE SEA 'PUBLIC STATIC CLASS'
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Users.AnyAsync()) return;

            var admin = new User
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                PasswordHash = BC.HashPassword("admin123"),
                Role = UserRole.Admin,
                Credits = 9999999
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}