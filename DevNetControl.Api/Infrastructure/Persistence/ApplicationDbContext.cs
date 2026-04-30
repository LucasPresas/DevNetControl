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
    public DbSet<Plan> Plans { get; set; }
    public DbSet<SessionLog> SessionLogs { get; set; }
    public DbSet<NodeAccess> NodeAccesses { get; set; }
    public DbSet<PlanAccess> PlanAccesses { get; set; }

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

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasOne(p => p.Tenant)
                .WithMany(t => t.Plans)
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionLog>(entity =>
        {
            entity.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.User)
                .WithMany(u => u.Sessions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<NodeAccess>(entity =>
        {
            entity.HasIndex(na => new { na.NodeId, na.UserId }).IsUnique();

            entity.HasOne(na => na.Node)
                .WithMany(n => n.AllowedUsers)
                .HasForeignKey(na => na.NodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(na => na.User)
                .WithMany()
                .HasForeignKey(na => na.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanAccess>(entity =>
        {
            entity.HasIndex(pa => new { pa.PlanId, pa.UserId }).IsUnique();

            entity.HasOne(pa => pa.Plan)
                .WithMany(p => p.AllowedUsers)
                .HasForeignKey(pa => pa.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        if (_currentTenantId.HasValue)
        {
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<VpsNode>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<CreditTransaction>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<Plan>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
            modelBuilder.Entity<SessionLog>().HasQueryFilter(e => e.TenantId == _currentTenantId.Value);
        }
    }
}
