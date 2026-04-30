using System;

namespace DevNetControl.Api.Domain
{
    public class NodeAccess
    {
        public Guid Id { get; set; }
        public Guid NodeId { get; set; }
        public Guid UserId { get; set; }

        public VpsNode Node { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
