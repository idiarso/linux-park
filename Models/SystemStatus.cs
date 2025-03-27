using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    public class SystemStatus
    {
        public bool IsOnline { get; set; }
        public DateTime LastChecked { get; set; }
        public Dictionary<string, bool> Components { get; set; } = new Dictionary<string, bool>();
    }
} 