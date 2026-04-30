using System;
using System.Collections.Generic;

namespace DevNetControl.Api.Domain
{
    public class Plan
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationHours { get; set; } = 720;
        public decimal CreditCost { get; set; } = 0;
        public int MaxConnections { get; set; } = 1;
        public int MaxDevices { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public bool IsTrial => CreditCost == 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<PlanAccess> AllowedUsers { get; set; } = new List<PlanAccess>();
        public Tenant Tenant { get; set; } = null!;
    }
}
