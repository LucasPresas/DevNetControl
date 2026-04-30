using System;
using System.Collections.Generic;

namespace DevNetControl.Api.Domain
{
    public enum UserRole
    {
        SuperAdmin,
        Admin,
        Reseller,
        SubReseller,
        Customer
    }

    public class User
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public decimal Credits { get; set; } = 0;
        public int MaxDevices { get; set; } = 1;
        public DateTime? ServiceExpiry { get; set; }

        public bool IsTrial { get; set; }
        public DateTime? TrialExpiry { get; set; }
        public bool IsProvisionedOnVps { get; set; }
        public bool IsActive { get; set; } = true;

        public Guid? PlanId { get; set; }
        public Plan? Plan { get; set; }

        public Guid? ParentId { get; set; }
        public User? Parent { get; set; }
        public ICollection<User> Subordinates { get; set; } = new List<User>();
        public ICollection<VpsNode> OwnedNodes { get; set; } = new List<VpsNode>();
        public ICollection<SessionLog> Sessions { get; set; } = new List<SessionLog>();
        public ICollection<PlanAccess> PlanAccesses { get; set; } = new List<PlanAccess>();

        public Tenant Tenant { get; set; } = null!;
    }
}
