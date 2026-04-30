using System;
using System.Collections.Generic;

namespace DevNetControl.Api.Domain
{
    public class VpsNode
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string IP { get; set; } = string.Empty;
        public int SshPort { get; set; } = 22;
        public string label { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;

        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
        public Tenant Tenant { get; set; } = null!;
        public ICollection<NodeAccess> AllowedUsers { get; set; } = new List<NodeAccess>();
    }
}
