using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using ParkIRC.Services;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

        public IActionResult PrintTicket()
        {
            // This is a page to show the ticket printing interface
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrintTicket(string vehicleNumber)
        {
            if (string.IsNullOrEmpty(vehicleNumber))
            {
                return BadRequest("Vehicle number is required");
            }

            // Find the latest transaction for this vehicle
            var transaction = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .Where(t => t.Vehicle != null && t.Vehicle.VehicleNumber == vehicleNumber)
                .OrderByDescending(t => t.EntryTime)
                .FirstOrDefaultAsync();

            if (transaction == null)
            {
                return NotFound("No transaction found for this vehicle");
            }
            
            if (transaction.Vehicle == null)
            {
                return NotFound("Vehicle information not found for this transaction");
            }

            // Create a ticket model
            var ticket = new ParkingTicket
            {
                TicketNumber = transaction.TransactionNumber,
                VehicleNumber = vehicleNumber,
                EntryTime = transaction.EntryTime,
                ParkingSpaceNumber = transaction.ParkingSpace?.SpaceNumber ?? "Unknown",
                VehicleType = transaction.Vehicle.VehicleType ?? "Unknown",
                // Add additional required properties for database validation
                BarcodeData = transaction.TransactionNumber,
                IssueTime = DateTime.Now,
                ShiftId = 1 // Default value, you might want to get the current shift
            };

            try
            {
                // Print ticket
                var success = await _printerService.PrintTicket(ticket.TicketNumber, ticket.EntryTime.ToString("dd/MM/yyyy HH:mm:ss"));
                if (!success)
                {
                    _logger.LogError("Failed to print ticket {TicketNumber}", ticket.TicketNumber);
                    return StatusCode(500, "Failed to print ticket");
                }
                
                return Json(new { success = true, message = "Tiket berhasil dicetak" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
    }
} 