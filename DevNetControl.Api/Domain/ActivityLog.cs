using System;
using System.ComponentModel.DataAnnotations;

namespace DevNetControl.Api.Domain;

public enum ActivityActionType
{
    UserCreated,
    UserDeleted,
    UserSuspended,
    UserUpdated,
    ResellerCreated,
    SubResellerCreated,
    CreditsTransferred,
    CreditsLoaded,
    CreditsConsumed,
    PlanAssigned,
    PlanChanged,
    ServiceExtended,
    NodeAccessGranted,
    NodeAccessRevoked,
    PlanAccessGranted,
    PlanAccessRevoked,
    BulkOperation,
    Login,
    Logout
}

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public ActivityActionType ActionType { get; set; }

    [Required]
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = null!;

    [MaxLength(100)]
    public string ActorUserName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ActorRole { get; set; } = string.Empty;

    public Guid? TargetUserId { get; set; }
    public User? TargetUser { get; set; }

    [MaxLength(100)]
    public string? TargetUserName { get; set; }

    public int CreditsConsumed { get; set; }

    public int CreditsBalanceBefore { get; set; }

    public int CreditsBalanceAfter { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Details { get; set; }

    public Guid? PlanId { get; set; }
    public Plan? Plan { get; set; }

    [MaxLength(100)]
    public string? PlanName { get; set; }

    public Guid? NodeId { get; set; }
    public VpsNode? Node { get; set; }

    [MaxLength(100)]
    public string? NodeLabel { get; set; }

    [Required]
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
