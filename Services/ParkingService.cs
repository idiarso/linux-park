using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace ParkIRC.Services
{
    public class ParkingService : IParkingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ParkingService> _logger;

        public ParkingService(ApplicationDbContext context, ILogger<ParkingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ParkingSpace> AssignParkingSpaceAsync(Vehicle vehicle)
        {
            return await AssignParkingSpace(vehicle);
        }

        public async Task<ParkingTransaction> ProcessExitAsync(string vehicleNumber, string paymentMethod)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.ParkingSpace)
                .FirstOrDefaultAsync(v => v.VehicleNumber == vehicleNumber && v.IsParked);

            if (vehicle == null)
            {
                throw new InvalidOperationException("Vehicle not found or not currently parked.");
            }

            var transaction = await ProcessCheckout(vehicle);
            transaction.PaymentMethod = paymentMethod;
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return user != null;
        }

        public async Task<ParkingTransaction> ProcessCheckout(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            var exitTime = DateTime.UtcNow;
            var duration = exitTime - vehicle.EntryTime;
            var hours = Math.Ceiling(duration.TotalHours);
            var hourlyRate = vehicle.ParkingSpace?.HourlyRate ?? 0m;
            decimal totalAmount = (decimal)hours * hourlyRate;

            var transaction = new ParkingTransaction
            {
                VehicleId = vehicle.Id,
                ParkingSpaceId = vehicle.ParkingSpaceId ?? 0,
                TransactionNumber = GenerateTransactionNumber(),
                EntryTime = vehicle.EntryTime,
                ExitTime = exitTime,
                HourlyRate = hourlyRate,
                Amount = totalAmount,
                TotalAmount = totalAmount,
                PaymentStatus = "Paid",
                PaymentMethod = "Cash",
                PaymentTime = exitTime,
                Vehicle = vehicle,
                ParkingSpace = vehicle.ParkingSpace
            };

            if (vehicle.ParkingSpace != null)
            {
                vehicle.ParkingSpace.IsOccupied = false;
                vehicle.ParkingSpace.CurrentVehicle = null;
                vehicle.ParkingSpace.LastOccupiedTime = exitTime;
            }
            vehicle.ParkingSpace = null;
            vehicle.ParkingSpaceId = null;

            _context.ParkingTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return transaction;
        }

        public async Task<ParkingSpace> AssignParkingSpace(Vehicle vehicle)
        {
            _logger.LogInformation($"Assigning parking space for vehicle {vehicle.VehicleNumber}");
            
            try
            {
                // Check if any parking spaces exist
                var parkingSpacesCount = await _context.ParkingSpaces.CountAsync();
                _logger.LogInformation($"Found {parkingSpacesCount} total parking spaces");
                
                if (parkingSpacesCount == 0)
                {
                    _logger.LogWarning("No parking spaces found in database, creating default spaces");
                    
                    // Create default parking spaces if none exist
                    var defaultSpaces = new List<ParkingSpace>();
                    for (int i = 1; i <= 10; i++)
                    {
                        defaultSpaces.Add(new ParkingSpace
                        {
                            SpaceNumber = $"A{i:D2}",
                            SpaceType = "Standard",
                            IsOccupied = false,
                            HourlyRate = 5000M,
                            IsReserved = false,
                            Location = "Default"
                        });
                    }
                    
                    await _context.ParkingSpaces.AddRangeAsync(defaultSpaces);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created 10 default parking spaces");
                }
                
                // Try to find an available space with direct query
                var availableSpace = await _context.ParkingSpaces
                    .Where(p => !p.IsOccupied && !p.IsReserved)
                    .FirstOrDefaultAsync();

                if (availableSpace == null)
                {
                    _logger.LogWarning("No available parking spaces found, creating an overflow space");
                    
                    // Create an overflow space if needed
                    var overflowSpace = new ParkingSpace
                    {
                        SpaceNumber = $"OVF-{DateTime.Now:yyyyMMddHHmmss}",
                        SpaceType = "Overflow",
                        IsOccupied = false,
                        HourlyRate = 5000M,
                        IsReserved = false,
                        Location = "Overflow"
                    };
                    
                    await _context.ParkingSpaces.AddAsync(overflowSpace);
                    await _context.SaveChangesAsync();
                    
                    availableSpace = overflowSpace;
                    _logger.LogInformation($"Created overflow space: {overflowSpace.SpaceNumber}");
                }

                _logger.LogInformation($"Assigning space {availableSpace.SpaceNumber} to vehicle {vehicle.VehicleNumber}");
                
                // Mark space as occupied but update database separately from vehicle
                availableSpace.IsOccupied = true;
                availableSpace.CurrentVehicleId = null; // Don't set circular reference
                
                // Set vehicle parking space ID
                vehicle.ParkingSpaceId = availableSpace.Id;
                vehicle.EntryTime = DateTime.Now;

                // Update space first to avoid circular reference issues
                _context.ParkingSpaces.Update(availableSpace);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Successfully marked space {availableSpace.SpaceNumber} as occupied");
                
                return availableSpace;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error assigning parking space for vehicle {vehicle.VehicleNumber}");
                throw new InvalidOperationException($"Error assigning parking space: {ex.Message}", ex);
            }
        }

        public async Task<decimal> CalculateFee(Vehicle vehicle)
        {
            if (vehicle == null || vehicle.EntryTime == default)
            {
                throw new ArgumentException("Invalid vehicle or entry time");
            }

            var duration = DateTime.UtcNow - vehicle.EntryTime;
            var hours = Math.Ceiling(duration.TotalHours);
            var space = await _context.ParkingSpaces.FindAsync(vehicle.ParkingSpaceId);
            
            if (space == null)
            {
                throw new InvalidOperationException("Vehicle is not assigned to a parking space");
            }

            return (decimal)hours * space.HourlyRate;
        }

        private static string GenerateTransactionNumber()
        {
            return "TRX-" + DateTime.Now.ToString("yyyyMMdd") + "-" + 
                   Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        }
    }
}