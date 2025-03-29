using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkIRC.Services;

namespace ParkIRC.Controllers
{
    [Authorize]
    public class ArduinoViewController : Controller
    {
        private readonly ILogger<ArduinoViewController> _logger;
        private readonly IArduinoService _arduinoService;

        public ArduinoViewController(
            ILogger<ArduinoViewController> logger,
            IArduinoService arduinoService)
        {
            _logger = logger;
            _arduinoService = arduinoService;
        }

        [HttpGet]
        [Route("Arduino")]
        [Route("Arduino/Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
} 