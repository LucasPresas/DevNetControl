using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace DevNetControl.Api.Domain
{
    public enum UserRole
    {
        Admin,
        Reseller,
        SubReseller,
        Customer
    }

    public class User
    {
        public Guid Id {get; set;}
        public string UserName {get; set;} = string.Empty;
        public string PasswordHash {get; set;} = string.Empty;
        public UserRole Role {get; set;}
        public int Credits {get; set;} = 0;

        public Guid? ParentId {get; set;}
        public User? Parent {get; set;}
        public ICollection<User> Subordinates {get; set;} = new List<User>();
        public ICollection<VpsNode> OwnedNodes { get; set; } = new List<VpsNode>();
    }

}