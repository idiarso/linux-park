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
        Task OpenGate(string gateId);
        Task CloseGate(string gateId);
        Task<bool> IsGateOpen(string gateId);
        Task InitializeHardware();
        
        event EventHandler<ImageCapturedEventArgs> ImageCaptured;
        event EventHandler<CommandReceivedEventArgs> CommandReceived;
        event EventHandler<PushButtonEventArgs> OnPushButtonPressed;
    }
} 