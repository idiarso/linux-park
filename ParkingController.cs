using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ParkIRC.Data;
using ParkIRC.Models;

[ApiController]
[Route("api")]
public class ParkingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ParkingController> _logger;

    public ParkingController(ApplicationDbContext context, ILogger<ParkingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("parking")]
    public async Task<IActionResult> AddVehicle([FromBody] VehicleDto dto)
    {
        _logger.LogInformation($"Received request: {JsonConvert.SerializeObject(dto)}");

        if (dto == null || string.IsNullOrEmpty(dto.PlateNumber))
        {
            _logger.LogWarning("Invalid vehicle data");
            return BadRequest(new { success = false, message = "Invalid vehicle data" });
        }

        try
        {
            // Map DTO to entity
            var vehicle = new Vehicle
            {
                VehicleNumber = dto.PlateNumber,
                VehicleType = dto.VehicleType ?? "Unknown",
                TicketNumber = GenerateTicketNumber(),
                VehicleTypeId = GetVehicleTypeId(dto.VehicleType),
                EntryTime = DateTime.Now,
                PlateNumber = dto.PlateNumber,
                CreatedBy = "API",
                IsParked = true
            };

            // Save to database
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Vehicle saved successfully with ID: {vehicle.Id}");

            return Ok(new
            {
                success = true,
                message = "Vehicle added successfully",
                data = new
                {
                    id = vehicle.Id,
                    vehicleNumber = vehicle.VehicleNumber,
                    ticketNumber = vehicle.TicketNumber,
                    entryTime = vehicle.EntryTime
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving vehicle: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Failed to save vehicle data", error = ex.Message });
        }
    }

    [HttpGet("test-connection")]
    public IActionResult TestConnection()
    {
        return Ok(new
        {
            success = true,
            message = "Connection successful",
            timestamp = DateTime.Now,
            server = Environment.MachineName
        });
    }

    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles()
    {
        try
        {
            var vehicles = await _context.Vehicles
                .OrderByDescending(v => v.Id)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                count = vehicles.Count,
                data = vehicles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving vehicles: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Failed to retrieve vehicles", error = ex.Message });
        }
    }

    private string GenerateTicketNumber()
    {
        // Format: TKTyyyyMMddHHmmssffff
        return $"TKT{DateTime.Now:yyyyMMddHHmmssffff}";
    }

    private int GetVehicleTypeId(string vehicleType)
    {
        return vehicleType?.ToLower() switch
        {
            "car" => 1,
            "motorcycle" => 2,
            _ => 0
        };
    }
}

public class VehicleDto
{
    public string VehicleId { get; set; }
    public string PlateNumber { get; set; }
    public string VehicleType { get; set; }
    public DateTime? Timestamp { get; set; }
}

public class VehicleBasic
{
    public int Id { get; set; }
    public string VehicleNumber { get; set; }
    public string VehicleType { get; set; }
    public string TicketNumber { get; set; }
    public int VehicleTypeId { get; set; }
    public string DriverName { get; set; }
    public string PhoneNumber { get; set; }
    public string ContactNumber { get; set; }
    public DateTime EntryTime { get; set; }
} 