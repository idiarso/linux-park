using System;
using System.Collections.Generic;

namespace ParkIRC.Models
{
    /// <summary>
    /// Represents an event received from the Arduino gate controller
    /// </summary>
    public class GateEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public string EventData { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime? ProcessedTime { get; set; }
        public string ProcessedBy { get; set; }
    }

    /// <summary>
    /// Represents a pending vehicle entry created by the Arduino gate controller
    /// </summary>
    public class PendingVehicleEntry
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Status { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
        public int? VehicleId { get; set; }
        public string EntryGateId { get; set; }
        public string ImagePath { get; set; }
    }

    /// <summary>
    /// Request model for printing a ticket
    /// </summary>
    public class PrintTicketRequest
    {
        public string TicketNumber { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleType { get; set; }
    }

    /// <summary>
    /// Event model for gate events
    /// </summary>
    public class GateEventModel
    {
        public string EventType { get; set; }
        public DateTime EventTime { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
} 