using System;

namespace ParkIRC.Hardware
{
    public class ImageCapturedEventArgs : EventArgs
    {
        public string TicketId { get; }
        public string ImagePath { get; }
        public bool IsEntryImage { get; }
        public string Location { get; }
        public byte[] ImageData { get; }
        public DateTime Timestamp { get; }

        public ImageCapturedEventArgs(string ticketId, string imagePath, bool isEntryImage)
        {
            TicketId = ticketId;
            ImagePath = imagePath;
            IsEntryImage = isEntryImage;
            Location = isEntryImage ? "entry" : "exit";
            Timestamp = DateTime.UtcNow;
        }

        public ImageCapturedEventArgs(string location, byte[] imageData)
        {
            Location = location;
            ImageData = imageData;
            Timestamp = DateTime.UtcNow;
            IsEntryImage = location.Equals("entry", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class CommandReceivedEventArgs : EventArgs
    {
        public string Command { get; }
        public string Data { get; }
        public string Source { get; }
        public DateTime Timestamp { get; }

        public CommandReceivedEventArgs(string command, string data)
        {
            Command = command;
            Data = data;
            Timestamp = DateTime.UtcNow;
        }

        public CommandReceivedEventArgs(string command, string source, string data)
        {
            Command = command;
            Source = source;
            Data = data;
            Timestamp = DateTime.UtcNow;
        }
    }

    public class PushButtonEventArgs : EventArgs
    {
        public string GateId { get; }
        public DateTime Timestamp { get; }

        public PushButtonEventArgs(string gateId)
        {
            GateId = gateId;
            Timestamp = DateTime.UtcNow;
        }
    }
} 