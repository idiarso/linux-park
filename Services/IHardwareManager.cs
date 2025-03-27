using System;
using System.Threading.Tasks;

public interface IHardwareManager
{
    event EventHandler<PushButtonEventArgs> OnPushButtonPressed;
    Task OpenGate(string gateId);
    Task CloseGate(string gateId);
    Task<bool> IsGateOpen(string gateId);
    Task InitializeHardware();
} 