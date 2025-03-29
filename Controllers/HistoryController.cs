using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using ParkIRC.ViewModels;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(ApplicationDbContext context, ILogger<HistoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, string? status = null, string? vehicleNumber = null, int page = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Loading History page with startDate: {StartDate}, endDate: {EndDate}, status: {Status}, vehicleNumber: {VehicleNumber}, page: {Page}", 
                    startDate, endDate, status, vehicleNumber, page);
                
                var transactions = _context.ParkingTransactions
                    .Include(t => t.Vehicle)
                    .Include(t => t.ParkingSpace)
                    .AsQueryable();

                _logger.LogInformation("Initial transactions query created");

                // Filter by date range
                if (startDate.HasValue)
                {
                    transactions = transactions.Where(t => t.EntryTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    transactions = transactions.Where(t => t.EntryTime <= endDate.Value.AddDays(1));
                }
                
                // Filter by vehicle number
                if (!string.IsNullOrEmpty(vehicleNumber))
                {
                    // Normalize input to handle variations in format
                    var normalizedInput = vehicleNumber.Replace(" ", "").ToLower().Trim();
                    transactions = transactions.Where(t => 
                        t.Vehicle.VehicleNumber.Replace(" ", "").ToLower().Contains(normalizedInput));
                    
                    _logger.LogInformation("Filtering by vehicle number: {VehicleNumber}", vehicleNumber);
                }
                
                // Filter by transaction status
                if (!string.IsNullOrEmpty(status))
                {
                    if (status.ToLower() == "terparkir" || status == "active")
                    {
                        // Vehicles currently parked (no exit time)
                        transactions = transactions.Where(t => !t.ExitTime.HasValue && t.Status != "Cancelled");
                        _logger.LogInformation("Filtering for currently parked vehicles");
                    }
                    else if (status.ToLower() == "keluar" || status == "completed")
                    {
                        // Vehicles that have exited
                        transactions = transactions.Where(t => t.ExitTime.HasValue && t.Status != "Cancelled");
                        _logger.LogInformation("Filtering for exited vehicles");
                    }
                    else if (status == "cancelled")
                    {
                        // Cancelled transactions
                        transactions = transactions.Where(t => t.Status == "Cancelled" || t.PaymentStatus == "Cancelled");
                    }
                    else if (status == "all")
                    {
                        // No filtering by status
                    }
                }

                var totalCount = await transactions.CountAsync();
                _logger.LogInformation("Total transactions count: {Count}", totalCount);

                // Check for parked vehicles
                var parkedVehicles = await _context.Vehicles.Where(v => v.IsParked).CountAsync();
                _logger.LogInformation("Total parked vehicles: {Count}", parkedVehicles);

                var parkingActivities = await transactions
                    .OrderByDescending(t => t.EntryTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new ParkingActivityViewModel
                    {
                        Id = t.Id.ToString(),
                        VehicleNumber = t.Vehicle == null ? "Unknown" : t.Vehicle.VehicleNumber,
                        VehicleType = t.Vehicle == null ? "Unknown" : t.Vehicle.VehicleType,
                        ParkingSpace = t.ParkingSpace == null ? "-" : t.ParkingSpace.SpaceNumber,
                        EntryTime = t.EntryTime,
                        ExitTime = t.ExitTime,
                        Duration = t.ExitTime.HasValue ? 
                            $"{(int)(t.ExitTime.Value - t.EntryTime).TotalHours}h {(t.ExitTime.Value - t.EntryTime).Minutes}m" : 
                            "In Progress",
                        Amount = t.TotalAmount,
                        Status = t.Status ?? t.PaymentStatus,
                        PaymentMethod = t.PaymentMethod ?? "-"
                    })
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} parking activities for display", parkingActivities.Count);

                // Store filter data in ViewData for maintaining state
                ViewData["CurrentFilter"] = status ?? "";
                ViewData["VehicleNumber"] = vehicleNumber ?? "";
                ViewData["StartDate"] = startDate;
                ViewData["EndDate"] = endDate;
                ViewData["CurrentPage"] = page;
                ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewData["ParkedVehicles"] = parkedVehicles;
                
                // Pass status types using the TransactionStatusType class
                ViewData["StatusTypes"] = TransactionStatusType.GetStatusTypes();

                return View(parkingActivities);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error when loading history page");
                return View("Error", new ErrorViewModel 
                { 
                    Message = "Terjadi kesalahan saat memuat riwayat parkir: " + ex.Message,
                    RequestId = HttpContext.TraceIdentifier
                });
            }
        }

        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .FirstOrDefaultAsync(m => m.Id.ToString() == id);
            
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        public async Task<IActionResult> Export(DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = _context.ParkingTransactions
                .Include(t => t.Vehicle)
                .Include(t => t.ParkingSpace)
                .AsQueryable();

            if (startDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                transactions = transactions.Where(t => t.EntryTime <= endDate.Value.AddDays(1));
            }

            var data = await transactions
                .OrderByDescending(t => t.EntryTime)
                .Select(t => new 
                {
                    TransactionNumber = t.TransactionNumber,
                    VehicleNumber = t.Vehicle == null ? "Unknown" : t.Vehicle.VehicleNumber,
                    VehicleType = t.Vehicle == null ? "Unknown" : t.Vehicle.VehicleType,
                    EntryTime = t.EntryTime,
                    ExitTime = t.ExitTime,
                    Duration = t.ExitTime.HasValue ? 
                        $"{(int)(t.ExitTime.Value - t.EntryTime).TotalHours}h {(t.ExitTime.Value - t.EntryTime).Minutes}m" : 
                        "In Progress",
                    Amount = t.TotalAmount,
                    PaymentStatus = t.PaymentStatus,
                    PaymentMethod = t.PaymentMethod
                })
                .ToListAsync();

            // Convert to CSV
            var csv = "Transaction Number,Vehicle Number,Vehicle Type,Entry Time,Exit Time,Duration,Amount,Payment Status,Payment Method\n";
            foreach (var item in data)
            {
                csv += $"{item.TransactionNumber},{item.VehicleNumber},{item.VehicleType},{item.EntryTime},{item.ExitTime},{item.Duration},{item.Amount},{item.PaymentStatus},{item.PaymentMethod}\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", $"parking_transactions_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
} 