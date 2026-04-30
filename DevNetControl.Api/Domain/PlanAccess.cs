using System;

namespace DevNetControl.Api.Domain
{
    public class PlanAccess
    {
        public Guid Id { get; set; }
        public Guid PlanId { get; set; }
        public Guid UserId { get; set; }

        public Plan Plan { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
