using System;

namespace DevNetControl.Api.Domain
{
    public class SessionLog
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public string? UserAgent { get; set; } = string.Empty;
        public string NodeIp { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public Tenant Tenant { get; set; } = null!;
        public User? User { get; set; }
    }
}
