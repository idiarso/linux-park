using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkIRC.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class PrismaController : Controller
    {
        private readonly PrismaService _prismaService;
        private readonly ILogger<PrismaController> _logger;

        public PrismaController(PrismaService prismaService, ILogger<PrismaController> logger)
        {
            _prismaService = prismaService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> AvailableSpaces()
        {
            try
            {
                string scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "scripts", "get-spaces.js");
                string result = await _prismaService.ExecutePrismaQuery(scriptPath);
                
                // Parse result to JSON
                var spaces = System.Text.Json.JsonSerializer.Deserialize<object>(result);
                
                return View(spaces);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving available spaces: {ex.Message}");
                return StatusCode(500, new { error = "Failed to retrieve available spaces" });
            }
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
} 