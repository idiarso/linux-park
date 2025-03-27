using System;
using System.Threading.Tasks;

namespace ParkIRC.Hardware
{
    public interface IHardwareManager : IAsyncDisposable
    {
        Task<bool> InitializeAsync();
        Task<bool> OpenEntryGate();
        Task<bool> OpenExitGate();
        Task<string> CaptureImageAsync(string location);
        Task<bool> PrintTicketAsync(string ticketNumber, string entryTime);
        Task<bool> PrintReceiptAsync(string ticketNumber, string exitTime, decimal amount);
        Task<bool> SendCommandAsync(string command);
        
        event EventHandler<ImageCapturedEventArgs> ImageCaptured;
        event EventHandler<CommandReceivedEventArgs> CommandReceived;
    }
} 