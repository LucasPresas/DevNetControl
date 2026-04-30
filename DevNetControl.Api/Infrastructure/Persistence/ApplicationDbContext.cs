using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly Guid? _currentTenantId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, Guid? currentTenantId) : base(options)
    {
        _currentTenantId = currentTenantId;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<VpsNode> VpsNodes { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Subdomain).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VpsNode>(entity =>
        {
            entity.HasOne(v => v.Tenant)
                .WithMany(t => t.VpsNodes)
                .HasForeignKey(v => v.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreditTransaction>(entity =>
        {
            entity.HasOne(t => t.FromUser)
                .WithMany()
                .HasForeignKey(t => t.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ToUser)
                .WithMany()
                .HasForeignKey(t => t.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        if (_currentTenantId.HasValue)
        {
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<VpsNode>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<CreditTransaction>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
        }
    }
}
