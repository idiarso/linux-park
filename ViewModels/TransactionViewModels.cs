using System;
using System.Collections.Generic;

namespace ParkIRC.ViewModels
{
    // Existing ViewModels stay the same
    
    /// <summary>
    /// Defines the available transaction status types and their display values
    /// </summary>
    public static class TransactionStatusType
    {
        public const string Active = "Active";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string Pending = "Pending";
        
        public static Dictionary<string, string> GetStatusTypes()
        {
            return new Dictionary<string, string>
            {
                { Active, "Parkir (Aktif)" },
                { Completed, "Selesai" },
                { Cancelled, "Dibatalkan" },
                { Pending, "Tertunda" }
            };
        }
        
        public static string GetDisplayName(string statusCode)
        {
            if (string.IsNullOrEmpty(statusCode))
                return "Unknown";
                
            var statusTypes = GetStatusTypes();
            return statusTypes.ContainsKey(statusCode) ? statusTypes[statusCode] : statusCode;
        }
        
        public static string GetStatusBadgeClass(string statusCode)
        {
            return statusCode switch
            {
                Active => "bg-warning text-dark",
                Completed => "bg-success",
                Cancelled => "bg-danger",
                Pending => "bg-info",
                _ => "bg-secondary"
            };
        }
    }
} 