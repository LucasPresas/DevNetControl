using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevNetControl.Api.Domain
{
    public class VpsNode
    {
        public Guid Id { get; set; }
        public string IP { get; set; } = string.Empty;
        public int SshPort { get; set; } = 22;
        public string label { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty; //Contraseña cifrada

        public Guid OwnerId { get; set; }
        public User Owner { get; set; } = null!;
    }


}    