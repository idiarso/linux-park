using System.ComponentModel.DataAnnotations;

namespace ParkIRC.ViewModels
{
    public class EntryRequest
    {
        [Required(ErrorMessage = "Entry Gate ID is required.")]
        public string EntryGateId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle number is required.")]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle type is required.")]
        public string VehicleType { get; set; } = string.Empty;
    }
}
