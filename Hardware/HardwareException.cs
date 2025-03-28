using System;

namespace ParkIRC.Hardware
{
    public class HardwareException : Exception
    {
        public HardwareException() : base() { }
        
        public HardwareException(string message) : base(message) { }
        
        public HardwareException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
} 