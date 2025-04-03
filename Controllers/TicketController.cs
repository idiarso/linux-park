using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkIRC.Data;
using ParkIRC.Models;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace ParkIRC.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TicketController> _logger;

        public TicketController(ApplicationDbContext context, ILogger<TicketController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/tickets/{ticketNumber}
        [HttpGet("{ticketNumber}")]
        public async Task<IActionResult> GetTicket(string ticketNumber)
        {
            try
            {
                var ticket = await _context.CaptureTickets
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

                if (ticket == null)
                {
                    return NotFound(new { message = "Tiket tidak ditemukan" });
                }

                return Ok(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ticket {TicketNumber}", ticketNumber);
                return StatusCode(500, new { message = "Terjadi kesalahan saat mengambil data tiket" });
            }
        }

        // POST /api/tickets/validate
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateTicket([FromBody] ValidateTicketRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.TicketNumber))
                {
                    return BadRequest(new { message = "Nomor tiket tidak boleh kosong" });
                }

                var ticket = await _context.CaptureTickets
                    .FirstOrDefaultAsync(t => t.TicketNumber == request.TicketNumber);

                if (ticket == null)
                {
                    return NotFound(new { message = "Tiket tidak ditemukan" });
                }

                if (ticket.Status == "keluar")
                {
                    return BadRequest(new { message = "Tiket sudah digunakan" });
                }

                return Ok(new { isValid = true, ticket });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ticket {TicketNumber}", request.TicketNumber);
                return StatusCode(500, new { message = "Terjadi kesalahan saat memvalidasi tiket" });
            }
        }

        // PUT /api/tickets/{ticketNumber}/exit
        [HttpPut("{ticketNumber}/exit")]
        public async Task<IActionResult> UpdateExitStatus(string ticketNumber)
        {
            try
            {
                var ticket = await _context.CaptureTickets
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber);

                if (ticket == null)
                {
                    return NotFound(new { message = "Tiket tidak ditemukan" });
                }

                ticket.Status = "keluar";
                ticket.ExitTimestamp = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Status tiket berhasil diupdate",
                    ticket 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exit status for ticket {TicketNumber}", ticketNumber);
                return StatusCode(500, new { message = "Terjadi kesalahan saat mengupdate status tiket" });
            }
        }
    }
} 