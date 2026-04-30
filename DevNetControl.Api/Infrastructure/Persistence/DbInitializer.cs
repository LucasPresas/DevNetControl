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

            // Crear Tenant especial para SuperAdmin si no existe
            var superAdminTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var platformTenant = await context.Tenants.FindAsync(superAdminTenantId);
            
            if (platformTenant == null)
            {
                platformTenant = new Tenant
                {
                    Id = superAdminTenantId,
                    Name = "Platform",
                    Subdomain = "platform",
                    AdminEmail = "platform@devnetcontrol.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Tenants.Add(platformTenant);
                await context.SaveChangesAsync();
            }

            // Crear SuperAdmin si no existe
            if (!await context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var superAdmin = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = superAdminTenantId,
                    UserName = "superadmin",
                    PasswordHash = BC.HashPassword("superadmin123"),
                    Role = UserRole.SuperAdmin,
                    Credits = 9999999
                };

                context.Users.Add(superAdmin);
                await context.SaveChangesAsync();
            }

            if (await context.Tenants.CountAsync() > 1) return;

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
