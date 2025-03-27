using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
} 