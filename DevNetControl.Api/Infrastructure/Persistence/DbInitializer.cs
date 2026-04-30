using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;
using BC = BCrypt.Net.BCrypt;

namespace DevNetControl.Api.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            if (await context.Tenants.AnyAsync()) return;

            var defaultTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Default Tenant",
                Subdomain = "default",
                AdminEmail = "admin@devnetcontrol.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Tenants.Add(defaultTenant);
            await context.SaveChangesAsync();

            var admin = new User
            {
                Id = Guid.NewGuid(),
                TenantId = defaultTenant.Id,
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
