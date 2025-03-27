using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class VehicleIntegrationController : ControllerBase
{
    private readonly VehicleIntegrationService _integrationService;
    private readonly ILogger<VehicleIntegrationController> _logger;

    public VehicleIntegrationController(
        VehicleIntegrationService integrationService,
        ILogger<VehicleIntegrationController> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    [HttpPost("entry")]
    public async Task<IActionResult> ProcessEntry([FromBody] VehicleEntryRequest request)
    {
        if (string.IsNullOrEmpty(request.VehicleNumber))
        {
            return BadRequest("Vehicle number is required");
        }

        try
        {
            var result = await _integrationService.ProcessVehicleEntry(request);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing vehicle entry: {ex.Message}");
            return StatusCode(500, "Internal server error");
        }
    }
}

public class VehicleEntryRequest
{
    public string VehicleNumber { get; set; } = string.Empty;
    public string VehicleType { get; set; } = string.Empty;
    public string GateId { get; set; } = string.Empty;
    public DateTime EntryTime { get; set; }
    public string EntryGateId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
} 