namespace ParkIRC.Models
{
    public class VehicleEntryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string TicketNumber { get; set; }
        public string ImagePath { get; set; }
        public bool PrintStatus { get; set; }
    }
} 