using System.Threading.Tasks;

namespace ParkIRC.Services.Interfaces
{
    public interface ICameraService
    {
        Task<byte[]> CaptureImage();
        Task<string> SaveImage(byte[] imageData, string filename);
    }
} 