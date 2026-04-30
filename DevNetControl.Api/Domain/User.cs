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

        public Guid? ParentId { get; set; }
        public User? Parent { get; set; }
        public ICollection<User> Subordinates { get; set; } = new List<User>();
        public ICollection<VpsNode> OwnedNodes { get; set; } = new List<VpsNode>();

        public Tenant Tenant { get; set; } = null!;
    }
}
