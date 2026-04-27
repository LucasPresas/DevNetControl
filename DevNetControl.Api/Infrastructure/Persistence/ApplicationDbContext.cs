using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
        public DbSet<User> Users{ get; set; }
        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            base.OnModelCreating(modelbuilder);
            modelbuilder.Entity<User>()                
                .HasOne(u => u.Parent)
                .WithMany(u => u.Subordinates)
                .HasForeignKey(u => u.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        }     


        
        
    }
}