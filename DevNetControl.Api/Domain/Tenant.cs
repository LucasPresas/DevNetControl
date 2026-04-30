using System;
using System.Collections.Generic;

namespace DevNetControl.Api.Domain
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subdomain { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public decimal TrialMaxHours { get; set; } = 2;
        public int TrialMaxPerReseller { get; set; } = 5;
        public decimal CreditsPerDevice { get; set; } = 1;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<VpsNode> VpsNodes { get; set; } = new List<VpsNode>();
        public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    }
}
