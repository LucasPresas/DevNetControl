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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Filtros Globales corregidos para evitar comparaciones Guid == null[cite: 1]
        var tenantId = _currentTenantId ?? Guid.Empty;
        bool hasTenant = _currentTenantId.HasValue;

        modelBuilder.Entity<User>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);
        
        // CORRECCIÓN CS8073: Usamos Guid.Empty para representar planes/nodos globales[cite: 1]
        modelBuilder.Entity<VpsNode>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId || e.TenantId == Guid.Empty);
        modelBuilder.Entity<Plan>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId || e.TenantId == Guid.Empty);
        
        modelBuilder.Entity<CreditTransaction>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);
        modelBuilder.Entity<SessionLog>().HasQueryFilter(e => !hasTenant || e.TenantId == tenantId);
        
        modelBuilder.Entity<NodeAccess>().HasQueryFilter(e => !hasTenant || e.Node.TenantId == tenantId);
        modelBuilder.Entity<PlanAccess>().HasQueryFilter(e => !hasTenant || e.Plan.TenantId == tenantId);
    }
}