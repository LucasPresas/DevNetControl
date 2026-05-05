using Microsoft.EntityFrameworkCore;
using DevNetControl.Api.Domain;

namespace DevNetControl.Api.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly Guid? _currentTenantId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, Guid? currentTenantId = null) 
        : base(options)
    {
        _currentTenantId = currentTenantId;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<VpsNode> VpsNodes { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<CreditTransaction> CreditTransactions { get; set; }
    public DbSet<SessionLog> SessionLogs { get; set; }
    public DbSet<NodeAccess> NodeAccesses { get; set; }
    public DbSet<PlanAccess> PlanAccesses { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtros Globales corregidos para evitar comparaciones Guid == null[cite: 1]
        var tenantId = _currentTenantId ?? Guid.Empty;
        bool hasTenant = _currentTenantId.HasValue;

        modelBuilder.Entity<User>()
            .HasOne(u => u.Parent)
            .WithMany(u => u.Subordinates)
            .HasForeignKey(u => u.ParentId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Tenant)
            .WithMany(t => t.Users)
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Plan)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.PlanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<User>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);

        modelBuilder.Entity<CreditTransaction>()
            .HasOne(ct => ct.SourceUser)
            .WithMany()
            .HasForeignKey(ct => ct.SourceUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CreditTransaction>()
            .HasOne(ct => ct.TargetUser)
            .WithMany()
            .HasForeignKey(ct => ct.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // CORRECCIÓN CS8073: Usamos Guid.Empty para representar planes/nodos globales[cite: 1]
        modelBuilder.Entity<VpsNode>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId || e.TenantId == Guid.Empty);
        modelBuilder.Entity<Plan>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId || e.TenantId == Guid.Empty);
        
        modelBuilder.Entity<CreditTransaction>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);
        modelBuilder.Entity<SessionLog>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);

        modelBuilder.Entity<SessionLog>()
            .HasOne(sl => sl.User)
            .WithMany(sl => sl.Sessions)
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        modelBuilder.Entity<NodeAccess>().HasQueryFilter(e => !hasTenant || e.Node.TenantId == tenantId);
        modelBuilder.Entity<PlanAccess>().HasQueryFilter(e => !hasTenant || e.Plan.TenantId == tenantId);
        
        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.ActorUser)
            .WithMany()
            .HasForeignKey(al => al.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.TargetUser)
            .WithMany()
            .HasForeignKey(al => al.TargetUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.Plan)
            .WithMany()
            .HasForeignKey(al => al.PlanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(al => al.Node)
            .WithMany()
            .HasForeignKey(al => al.NodeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ActivityLog>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);
    }
}