using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;

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
                PasswordHash = "admin123", 
                Role = UserRole.Admin,
                Credits = 9999
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
    }
}