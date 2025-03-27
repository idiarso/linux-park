using System;
using System.Threading.Tasks;

namespace ParkIRC.Services
{
    public interface IRedundancyService
    {
        Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName, object context);
        bool IsUsingBackupDevice(string deviceType);
        Task<bool> SwitchToBackupDeviceAsync(string deviceType);
        Task<bool> SwitchToPrimaryDeviceAsync(string deviceType);
    }
} 