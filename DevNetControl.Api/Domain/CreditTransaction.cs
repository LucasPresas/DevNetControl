using System;
using System.Collections.Generic;

namespace DevNetControl.Api.Domain
{
    public class CreditTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Note { get; set; } = string.Empty;

        public Tenant Tenant { get; set; } = null!;
        public User FromUser { get; set; } = null!;
        public User ToUser { get; set; } = null!;
    }
}
