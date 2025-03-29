using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPrinterService _printerService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(ApplicationDbContext context, IPrinterService printerService, ILogger<TicketController> logger)
        {
            _context = context;
            _printerService = printerService;
            _logger = logger;
        }

        public async Task<IActionResult> PrintTicket()
        {
            // Get list of active vehicles for dropdown
            var activeVehicles = await _context.Vehicles
                .Where(v => v.IsParked)
                .OrderBy(v => v.VehicleNumber)
                .Select(v => new { v.VehicleNumber, v.VehicleType })
                .ToListAsync();
            
            ViewBag.ActiveVehicles = activeVehicles;
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintTicket(string vehicleNumber)
        {
            if (string.IsNullOrEmpty(vehicleNumber))
            {
                return Json(new { success = false, message = "Nomor kendaraan wajib diisi" });
            }

            try
            {
                // Find the vehicle
                var vehicle = await _context.Vehicles
                    .Where(v => v.VehicleNumber == vehicleNumber && v.IsParked)
                    .Include(v => v.ParkingSpace)
                    .FirstOrDefaultAsync();

                if (vehicle == null)
                {
                    _logger.LogWarning("Vehicle not found or not parked: {VehicleNumber}", vehicleNumber);
                    return Json(new { success = false, message = "Kendaraan tidak ditemukan atau tidak sedang parkir" });
                }

                // Find the latest transaction for this vehicle
                var transaction = await _context.ParkingTransactions
                    .Where(t => t.Vehicle != null && t.Vehicle.Id == vehicle.Id && t.ExitTime == null)
                    .OrderByDescending(t => t.EntryTime)
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    _logger.LogWarning("Active transaction not found for vehicle: {VehicleNumber}", vehicleNumber);
                    return Json(new { success = false, message = "Transaksi aktif tidak ditemukan untuk kendaraan ini" });
                }

                // Create a ticket model
                var ticket = new ParkingTicket
                {
                    TicketNumber = transaction.TicketNumber ?? transaction.TransactionNumber,
                    VehicleNumber = vehicle.VehicleNumber,
                    EntryTime = transaction.EntryTime,
                    ParkingSpaceNumber = vehicle.ParkingSpace?.SpaceNumber ?? "General",
                    VehicleType = vehicle.VehicleType ?? "Unknown",
                    // Add additional required properties for database validation
                    BarcodeData = transaction.TransactionNumber,
                    IssueTime = DateTime.Now,
                    ShiftId = 1 // Default value
                };

                // Format ticket content
                var ticketContent = $"TIKET PARKIR\n==============\nNo. Tiket: {ticket.TicketNumber}\nNo. Kendaraan: {ticket.VehicleNumber}\nWaktu Masuk: {ticket.EntryTime:yyyy-MM-dd HH:mm:ss}\nTipe Kendaraan: {ticket.VehicleType}\nLokasi: {ticket.ParkingSpaceNumber}\n\n";
                
                // Print ticket
                var success = await _printerService.PrintTicket(ticketContent);
                if (!success)
                {
                    _logger.LogError("Failed to print ticket {TicketNumber}", ticket.TicketNumber);
                    return Json(new { success = false, message = "Gagal mencetak tiket. Periksa printer Anda." });
                }
                
                _logger.LogInformation("Ticket printed successfully: {TicketNumber} for vehicle {VehicleNumber}", 
                    ticket.TicketNumber, ticket.VehicleNumber);
                
                return Json(new { 
                    success = true, 
                    message = "Tiket berhasil dicetak",
                    ticketNumber = ticket.TicketNumber,
                    vehicleNumber = ticket.VehicleNumber,
                    entryTime = ticket.EntryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    parkingSpace = ticket.ParkingSpaceNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing ticket for vehicle {VehicleNumber}", vehicleNumber);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetActiveVehicles()
        {
            try
            {
                var vehicles = await _context.Vehicles
                    .Where(v => v.IsParked)
                    .OrderBy(v => v.VehicleNumber)
                    .Select(v => new { v.VehicleNumber, v.VehicleType })
                    .ToListAsync();
                
                return Json(vehicles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active vehicles");
                return Json(new List<object>());
            }
        }
    }
} 