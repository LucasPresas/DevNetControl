using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DevNetControl.Api.Infrastructure.Persistence
{
    public class DbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Lógica inteligente para detectar el motor
            if (connectionString != null && connectionString.Contains("Data Source"))
            {
                builder.UseSqlite(connectionString);
            }
            else
            {
                builder.UseNpgsql(connectionString);
            }

            return new ApplicationDbContext(builder.Options);
        }
    }
}