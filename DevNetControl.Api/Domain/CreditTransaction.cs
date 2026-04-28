using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DevNetControl.Api.Domain
{
    public class CreditTransaction
    {
        public Guid Id { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Note { get; set; } = string.Empty;

        public User FromUser { get; set; } = null!; //navegacion
        public User ToUser { get; set; } = null!;
    }
}