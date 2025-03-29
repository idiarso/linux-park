# Arduino Barrier Gate and Thermal Printer Integration

This document outlines how to set up Arduino to handle barrier gate control and thermal printer operation in the parking management system.

## Hardware Requirements

- Arduino Mega 2560 or Arduino Uno (Mega recommended for more memory)
- Thermal Receipt Printer (58mm or 80mm)
- TTL to Serial adapter (if printer uses RS232)
- Relay module (for barrier gate control)
- IR sensors or inductive loops (for vehicle detection)
- Ethernet Shield or USB connection to host computer
- Push button (for manual gate control)

## Connection Options

### Option 1: Serial Connection (Recommended)

Connect Arduino to the entry gate computer via USB. The computer acts as the host, sending commands to Arduino and receiving status updates.

**Advantages:**
- Simpler setup, no network configuration needed
- More reliable connection
- Easier debugging
- Computer can buffer print jobs if needed

### Option 2: Direct Network Connection

Use an Ethernet shield to give Arduino its own IP address on the network.

**Advantages:**
- Arduino can operate independently
- Can be placed further from the host computer
- Multiple computers can communicate with it

## Wiring Diagram

```
[Thermal Printer] <---- TTL Serial ---> [Arduino]
                                           |
[Barrier Motor] <----- Relay Module <-----|
                                           |
[IR Sensors/Loop] <-------------------- Digital Pins
                                           |
[Push Button] <------------------------ Digital Pin
```

## Arduino Code

```cpp
/**
 * ParkIRC - Arduino Gate Controller with Thermal Printer
 * For barrier gate control and ticket printing
 */
#include <SoftwareSerial.h>
#include <Adafruit_Thermal.h>

// Pin definitions
#define PRINTER_RX_PIN 2
#define PRINTER_TX_PIN 3
#define BARRIER_RELAY_PIN 4
#define IR_SENSOR_ENTRY_PIN 5
#define IR_SENSOR_EXIT_PIN 6
#define PUSH_BUTTON_PIN 7

// Printer setup
SoftwareSerial printerSerial(PRINTER_RX_PIN, PRINTER_TX_PIN);
Adafruit_Thermal printer(&printerSerial);

// Vehicle detection states
bool vehicleDetected = false;
bool gateOpen = false;
unsigned long gateOpenTime = 0;
const unsigned long GATE_TIMEOUT = 10000; // 10 seconds timeout

// Communication buffer
String inputBuffer = "";
bool commandComplete = false;

void setup() {
  // Initialize serial communications
  Serial.begin(9600);
  printerSerial.begin(9600);
  
  // Initialize printer
  printer.begin();
  
  // Initialize pins
  pinMode(BARRIER_RELAY_PIN, OUTPUT);
  pinMode(IR_SENSOR_ENTRY_PIN, INPUT);
  pinMode(IR_SENSOR_EXIT_PIN, INPUT);
  pinMode(PUSH_BUTTON_PIN, INPUT_PULLUP);
  
  // Set initial state
  digitalWrite(BARRIER_RELAY_PIN, LOW);
  
  // Send ready signal
  Serial.println("READY:GATE_CONTROLLER");
}

void loop() {
  // Check for commands from the host computer
  checkSerialCommands();
  
  // Check for manual button press
  checkButtonPress();
  
  // Check vehicle sensors
  checkVehicleSensors();
  
  // Check if gate should be closed (timeout)
  checkGateTimeout();
  
  // Process any completed commands
  if (commandComplete) {
    processCommand();
    commandComplete = false;
    inputBuffer = "";
  }
}

void checkSerialCommands() {
  while (Serial.available() > 0) {
    char inChar = (char)Serial.read();
    
    // End of command marker
    if (inChar == '\n') {
      commandComplete = true;
      break;
    }
    
    // Add character to buffer
    inputBuffer += inChar;
  }
}

void processCommand() {
  // Command format: CMD:PARAM1:PARAM2
  int firstColon = inputBuffer.indexOf(':');
  
  if (firstColon == -1) {
    Serial.println("ERR:INVALID_COMMAND_FORMAT");
    return;
  }
  
  String command = inputBuffer.substring(0, firstColon);
  String params = inputBuffer.substring(firstColon + 1);
  
  if (command == "OPEN") {
    openGate();
    Serial.println("OK:GATE_OPENED");
  }
  else if (command == "CLOSE") {
    closeGate();
    Serial.println("OK:GATE_CLOSED");
  }
  else if (command == "STATUS") {
    reportStatus();
  }
  else if (command == "PRINT") {
    printTicket(params);
    Serial.println("OK:TICKET_PRINTED");
  }
  else {
    Serial.println("ERR:UNKNOWN_COMMAND");
  }
}

void checkButtonPress() {
  // Check if button is pressed (active low)
  if (digitalRead(PUSH_BUTTON_PIN) == LOW) {
    // Debounce
    delay(50);
    if (digitalRead(PUSH_BUTTON_PIN) == LOW) {
      // Button action - toggle gate
      if (gateOpen) {
        closeGate();
      } else {
        openGate();
        // Report to host computer
        Serial.println("EVENT:BUTTON_PRESS");
      }
      // Wait for button release
      while (digitalRead(PUSH_BUTTON_PIN) == LOW) {
        delay(10);
      }
    }
  }
}

void checkVehicleSensors() {
  // Check entry sensor
  bool vehiclePresent = (digitalRead(IR_SENSOR_ENTRY_PIN) == LOW);
  
  // Rising edge - vehicle newly detected
  if (vehiclePresent && !vehicleDetected) {
    vehicleDetected = true;
    Serial.println("EVENT:VEHICLE_DETECTED");
  }
  // Falling edge - vehicle has passed
  else if (!vehiclePresent && vehicleDetected) {
    vehicleDetected = false;
    Serial.println("EVENT:VEHICLE_PASSED");
  }
}

void checkGateTimeout() {
  // If gate is open and timeout has passed, close it
  if (gateOpen && (millis() - gateOpenTime > GATE_TIMEOUT)) {
    closeGate();
    Serial.println("EVENT:GATE_TIMEOUT");
  }
}

void openGate() {
  digitalWrite(BARRIER_RELAY_PIN, HIGH);
  gateOpen = true;
  gateOpenTime = millis();
}

void closeGate() {
  digitalWrite(BARRIER_RELAY_PIN, LOW);
  gateOpen = false;
}

void reportStatus() {
  String status = "STATUS:";
  status += gateOpen ? "OPEN:" : "CLOSED:";
  status += vehicleDetected ? "VEHICLE_PRESENT" : "NO_VEHICLE";
  
  Serial.println(status);
}

void printTicket(String ticketData) {
  // Reset printer to known state
  printer.wake();
  printer.setDefault();
  
  // Center text
  printer.justify('C');
  
  // Parse the ticket data for special formatting
  int currentPos = 0;
  int nextNewline = ticketData.indexOf('\n', currentPos);
  
  while (nextNewline >= 0) {
    String line = ticketData.substring(currentPos, nextNewline);
    processTicketLine(line);
    
    currentPos = nextNewline + 1;
    nextNewline = ticketData.indexOf('\n', currentPos);
  }
  
  // Process the last line if there is one
  if (currentPos < ticketData.length()) {
    processTicketLine(ticketData.substring(currentPos));
  }
  
  // Feed paper and cut
  printer.feed(3);
  printer.sleep();      // Tell printer to sleep
}

void processTicketLine(String line) {
  // Check for special formatting commands
  if (line.startsWith("TITLE:")) {
    printer.setSize('L');  // Large text
    printer.println(line.substring(6));
    printer.setSize('S');  // Back to small text
  }
  else if (line.startsWith("BARCODE:")) {
    String barcodeData = line.substring(8);
    printer.feed(1);
    // Print barcode (Code 128)
    printer.printBarcode(barcodeData.c_str(), CODE128);
    printer.feed(1);
  }
  else if (line.startsWith("QR:")) {
    String qrData = line.substring(3);
    printer.feed(1);
    // Print QR code with size 8
    printer.printQR(qrData.c_str(), 8);
    printer.feed(1);
  }
  else if (line.startsWith("LARGE:")) {
    printer.setSize('M');  // Medium text
    printer.println(line.substring(6));
    printer.setSize('S');  // Back to small text
  }
  else if (line.startsWith("LINE")) {
    printer.println("--------------------------------");
  }
  else {
    // Regular text
    printer.println(line);
  }
}
```

## Host Computer Integration

### Serial Communication Protocol

The Arduino communicates with the host computer using a simple text-based protocol:

1. **Commands from host to Arduino:**
   - `OPEN` - Open the barrier gate
   - `CLOSE` - Close the barrier gate
   - `STATUS` - Request current status
   - `PRINT:ticket_data` - Print a ticket

2. **Responses from Arduino to host:**
   - `OK:action` - Command was successful
   - `ERR:reason` - Command failed
   - `EVENT:type` - Something happened (button press, vehicle detected)
   - `STATUS:gate_state:vehicle_state` - Status report

3. **Ticket Format Example:**
   ```
   TITLE:PARKING TICKET
   LINE
   Date: 29-03-2025
   Time: 08:30:45
   LARGE:GATE 1
   LINE
   BARCODE:TKT202503290830
   Thank you
   ```

### C# Integration Code

Add this class to your C# codebase:

```csharp
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ParkIRC.Hardware
{
    public class ArduinoGateController : IDisposable
    {
        private readonly SerialPort _serialPort;
        private readonly ILogger<ArduinoGateController> _logger;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly string _portName;
        
        public ArduinoGateController(string portName, ILogger<ArduinoGateController> logger)
        {
            _portName = portName;
            _logger = logger;
            
            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 2000,
                WriteTimeout = 2000,
                NewLine = "\n"
            };
            
            _serialPort.DataReceived += SerialPort_DataReceived;
        }
        
        public async Task OpenAsync()
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _logger.LogInformation("Serial port {PortName} opened", _portName);
                    
                    // Wait for READY message
                    await Task.Delay(2000); // Give Arduino time to reset
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening serial port {PortName}", _portName);
                throw;
            }
        }
        
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string message = _serialPort.ReadLine().Trim();
                _logger.LogTrace("Received from Arduino: {Message}", message);
                
                if (message.StartsWith("EVENT:"))
                {
                    string eventType = message.Substring(6);
                    ProcessArduinoEvent(eventType);
                }
            }
            catch (TimeoutException)
            {
                // Ignore timeout exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading from Arduino");
            }
        }
        
        private void ProcessArduinoEvent(string eventType)
        {
            _logger.LogInformation("Arduino event: {EventType}", eventType);
            
            switch (eventType)
            {
                case "BUTTON_PRESS":
                    // Handle button press event
                    break;
                    
                case "VEHICLE_DETECTED":
                    // Handle vehicle detection
                    break;
                    
                case "VEHICLE_PASSED":
                    // Handle vehicle passed event
                    break;
            }
            
            // Raise events for subscribers
            ArduinoEvent?.Invoke(this, new ArduinoEventArgs(eventType));
        }
        
        public async Task<bool> OpenGateAsync()
        {
            return await SendCommandAsync("OPEN");
        }
        
        public async Task<bool> CloseGateAsync()
        {
            return await SendCommandAsync("CLOSE");
        }
        
        public async Task<bool> PrintTicketAsync(string ticketData)
        {
            // Format ticket data with special commands
            string formattedTicket = FormatTicketForPrinting(ticketData);
            
            return await SendCommandAsync($"PRINT:{formattedTicket}");
        }
        
        private string FormatTicketForPrinting(string ticketData)
        {
            // You can parse JSON or other format here to create the printer commands
            return ticketData.Replace("{BARCODE}", "BARCODE:");
        }
        
        private async Task<bool> SendCommandAsync(string command)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_serialPort.IsOpen)
                {
                    await OpenAsync();
                }
                
                _logger.LogTrace("Sending to Arduino: {Command}", command);
                _serialPort.WriteLine(command);
                
                // Wait for response with timeout
                string response = await ReadLineWithTimeoutAsync();
                
                return response.StartsWith("OK:");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to Arduino: {Command}", command);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private async Task<string> ReadLineWithTimeoutAsync()
        {
            var cancellationToken = new CancellationTokenSource(3000).Token;
            
            return await Task.Run(() => {
                try {
                    return _serialPort.ReadLine();
                }
                catch (TimeoutException) {
                    return "ERR:TIMEOUT";
                }
            }, cancellationToken);
        }
        
        public void Dispose()
        {
            _serialPort.DataReceived -= SerialPort_DataReceived;
            
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
            
            _serialPort.Dispose();
            _semaphore.Dispose();
        }
        
        // Event for Arduino events
        public event EventHandler<ArduinoEventArgs> ArduinoEvent;
    }
    
    public class ArduinoEventArgs : EventArgs
    {
        public string EventType { get; }
        
        public ArduinoEventArgs(string eventType)
        {
            EventType = eventType;
        }
    }
}
```

### Register in Dependency Injection

Add this to your `Program.cs`:

```csharp
// Add Arduino gate controller as singleton
builder.Services.AddSingleton<ArduinoGateController>(sp => {
    var logger = sp.GetRequiredService<ILogger<ArduinoGateController>>();
    var config = sp.GetRequiredService<IConfiguration>();
    
    string portName = config["Hardware:Arduino:PortName"] ?? "COM3";
    return new ArduinoGateController(portName, logger);
});
```

## Controller Update

Modify your `ParkingController.cs` to use the Arduino controller:

```csharp
// Add to the controller constructor parameters
private readonly ArduinoGateController _arduinoController;

public ParkingController(
    // ... existing dependencies
    ArduinoGateController arduinoController)
{
    // ... existing assignments
    _arduinoController = arduinoController;
    
    // Subscribe to Arduino events
    _arduinoController.ArduinoEvent += OnArduinoEvent;
}

private void OnArduinoEvent(object sender, ArduinoEventArgs e)
{
    if (e.EventType == "BUTTON_PRESS" || e.EventType == "VEHICLE_DETECTED")
    {
        // Handle entry event - can create pending vehicle entry
        // or trigger automatic entry processing
    }
}

// Update ProcessEntryAsync method
[HttpPost]
public async Task<IActionResult> ProcessEntryAsync([FromBody] VehicleEntryModel model)
{
    try
    {
        // ... existing validation and processing
        
        // Generate ticket data
        string ticketNumber = GenerateTicketNumber();
        DateTime entryTime = DateTime.Now;
        
        // Create formatted ticket for the printer
        string ticketData = $@"
TITLE:PARKING TICKET
LINE
Date: {entryTime:dd-MM-yyyy}
Time: {entryTime:HH:mm:ss}
LARGE:{model.VehicleNumber}
LINE
BARCODE:{ticketNumber}
Thank you for parking with us!
";
        
        // Print the ticket
        bool printSuccess = await _arduinoController.PrintTicketAsync(ticketData);
        
        // Open the gate
        bool gateSuccess = await _arduinoController.OpenGateAsync();
        
        // ... save to database and complete processing
        
        return Json(new { 
            success = true, 
            message = "Vehicle entry processed successfully",
            ticketNumber = ticketNumber,
            printSuccess = printSuccess,
            gateSuccess = gateSuccess
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing vehicle entry");
        return Json(new { success = false, message = "Error processing entry: " + ex.Message });
    }
}
```

## Configuration Settings

Add to your `appsettings.json`:

```json
"Hardware": {
  "Arduino": {
    "PortName": "COM3",
    "BaudRate": 9600,
    "AutoReconnect": true
  },
  "Printer": {
    "Type": "Thermal",
    "Width": 58,
    "CharactersPerLine": 32,
    "AutoCut": true
  }
}
```

## Troubleshooting

### Common Issues

1. **Arduino not responding**
   - Check USB connection
   - Verify correct COM port is selected
   - Check Arduino has correct code uploaded
   - Check signal LEDs on Arduino

2. **Printer not printing**
   - Check power to thermal printer
   - Verify printer connections to Arduino
   - Test printer directly
   - Check paper is loaded correctly

3. **Gate not operating**
   - Check relay connections
   - Verify power supply to gate motor
   - Test relay operation with multimeter
   - Check manual override works

### Testing

Use the Arduino Serial Monitor (9600 baud) to test commands:
- Send `OPEN` to open the gate
- Send `CLOSE` to close the gate
- Send `STATUS` to get current status
- Send a test print command

Example test print:
```
PRINT:TITLE:TEST TICKET\nLINE\nDate: 29-03-2025\nBARCODE:TEST123\nThis is a test
```

Berikut adalah perbaikan dan penjelasan dalam bahasa Indonesia:

```c#:Controllers/ArduinoController.cs
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ParkIRC.Models;
using ParkIRC.Services;
using System;

namespace ParkIRC.Controllers
{
    /// <summary>
    /// Controller API untuk komunikasi dengan Arduino
    /// Menangani perintah untuk membuka/menutup gerbang dan mengirim perintah print
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ArduinoController : ControllerBase
    {
        private readonly ILogger<ArduinoController> _logger;
        private readonly IArduinoService _arduinoService;
        private readonly IParkingService _parkingService;
        private readonly IPrinterService _printerService;

        public ArduinoController(
            ILogger<ArduinoController> logger,
            IArduinoService arduinoService,
            IParkingService parkingService,
            IPrinterService printerService)
        {
            _logger = logger;
            _arduinoService = arduinoService;
            _parkingService = parkingService;
            _printerService = printerService;
            
            // Mendaftarkan handler event dari Arduino
            _arduinoService.ArduinoEventReceived += ArduinoService_EventReceived;
        }

        /// <summary>
        /// Menangani event yang diterima dari Arduino
        /// </summary>
        private async void ArduinoService_EventReceived(object? sender, string eventData)
        {
            _logger.LogInformation("Event Arduino diterima: {EventData}", eventData);
            
            // Menangani berbagai jenis event
            if (eventData.StartsWith("EVENT:VEHICLE_DETECTED"))
            {
                await HandleVehicleDetected();
            }
            else if (eventData.StartsWith("EVENT:BUTTON_PRESS"))
            {
                await HandleButtonPress();
            }
            else if (eventData.StartsWith("EVENT:VEHICLE_PASSED"))
            {
                // Kendaraan telah melewati gerbang
                _logger.LogInformation("Kendaraan telah melewati gerbang");
            }
            else if (eventData.StartsWith("EVENT:GATE_TIMEOUT"))
            {
                // Timeout gerbang, gerbang ditutup secara otomatis
                _logger.LogInformation("Gerbang ditutup otomatis karena timeout");
            }
        }

        /// <summary>
        /// Menangani event ketika kendaraan terdeteksi
        /// </summary>
        private async Task HandleVehicleDetected()
        {
            try
            {
                // Membuat entri parkir baru
                var vehicleNumber = "TEMP-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                _logger.LogInformation("Mencoba membuat entri parkir untuk kendaraan: {VehicleNumber}", vehicleNumber);
                
                var entry = await _parkingService.CreateEntryAsync(vehicleNumber);
                
                if (entry == null)
                {
                    _logger.LogError("Gagal membuat entri parkir");
                    return;
                }
                
                // Mencetak tiket masuk
                var printResult = await _printerService.PrintEntryTicketAsync(entry);
                
                if (!printResult)
                {
                    _logger.LogWarning("Gagal mencetak tiket masuk untuk kendaraan: {VehicleNumber}", vehicleNumber);
                }
                
                // Membuka gerbang
                var gateResult = await _arduinoService.OpenGateAsync();
                
                if (!gateResult)
                {
                    _logger.LogError("Gagal membuka gerbang untuk kendaraan: {VehicleNumber}", vehicleNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menangani deteksi kendaraan");
            }
        }

        /// <summary>
        /// Menangani event ketika tombol ditekan
        /// </summary>
        private async Task HandleButtonPress()
        {
            try
            {
                _logger.LogInformation("Tombol ditekan, membuka gerbang");
                var result = await _arduinoService.OpenGateAsync();
                
                if (!result)
                {
                    _logger.LogError("Gagal membuka gerbang saat tombol ditekan");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menangani penekanan tombol");
            }
        }

        /// <summary>
        /// Mendapatkan status Arduino dan gerbang
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                // Menginisialisasi koneksi Arduino jika belum
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Error = "Koneksi Arduino gagal", Message = "Tidak dapat terhubung ke perangkat Arduino" });
                }
                
                var status = await _arduinoService.GetStatusAsync();
                
                if (string.IsNullOrEmpty(status))
                {
                    return StatusCode(500, new { Error = "Status tidak tersedia", Message = "Tidak dapat mendapatkan status dari Arduino" });
                }
                
                return Ok(new { Status = status, Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mendapatkan status Arduino");
                return StatusCode(500, new { Error = "Kesalahan komunikasi", Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Membuka gerbang parkir
        /// </summary>
        [HttpPost("gate/open")]
        public async Task<IActionResult> OpenGate()
        {
            try
            {
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.OpenGateAsync();
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal membuka gerbang" });
                }
                
                return Ok(new { Success = true, Message = "Gerbang berhasil dibuka", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat membuka gerbang");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Menutup gerbang parkir
        /// </summary>
        [HttpPost("gate/close")]
        public async Task<IActionResult> CloseGate()
        {
            try
            {
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.CloseGateAsync();
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal menutup gerbang" });
                }
                
                return Ok(new { Success = true, Message = "Gerbang berhasil ditutup", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat menutup gerbang");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }

        /// <summary>
        /// Mengirim perintah cetak ke printer melalui Arduino
        /// </summary>
        [HttpPost("print")]
        public async Task<IActionResult> SendPrintCommand([FromBody] PrintCommandModel model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.PrintData))
                {
                    return BadRequest(new { Success = false, Message = "Data cetak tidak boleh kosong" });
                }
                
                if (!await _arduinoService.InitializeAsync())
                {
                    _logger.LogError("Gagal menginisialisasi koneksi Arduino");
                    return StatusCode(503, new { Success = false, Message = "Koneksi Arduino gagal" });
                }
                
                var result = await _arduinoService.SendPrintCommandAsync(model.PrintData);
                
                if (!result)
                {
                    return StatusCode(500, new { Success = false, Message = "Gagal mengirim perintah cetak" });
                }
                
                return Ok(new { Success = true, Message = "Perintah cetak berhasil dikirim", Timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mengirim perintah cetak");
                return StatusCode(500, new { Success = false, Message = "Error saat berkomunikasi dengan Arduino" });
            }
        }
    }

    /// <summary>
    /// Model untuk mengirim perintah cetak
    /// </summary>
    public class PrintCommandModel
    {
        public string PrintData { get; set; } = string.Empty;
    }
}
```

```razor:Views/Arduino/Index.cshtml
@{
    ViewData["Title"] = "Kontrol Arduino";
}

<div class="container">
    <h1 class="mb-4">Panel Kontrol Arduino</h1>
    
    <div class="row mb-4">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Kontrol Gerbang</h5>
                </div>
                <div class="card-body">
                    <div class="mb-3">
                        <label class="form-label">Status Gerbang:</label>
                        <span id="arduino-status" class="ms-2 fw-bold">Memuat...</span>
                    </div>
                    <div class="d-flex gap-2">
                        <button id="open-gate-btn" class="btn btn-success">
                            <i class="bi bi-box-arrow-right"></i> Buka Gerbang
                        </button>
                        <button id="close-gate-btn" class="btn btn-danger">
                            <i class="bi bi-box-arrow-in-left"></i> Tutup Gerbang
                        </button>
                    </div>
                </div>
            </div>
        </div>
        
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">
                    <h5>Perintah Cetak</h5>
                </div>
                <div class="card-body">
                    <div class="mb-3">
                        <label for="print-data" class="form-label">Data Cetak:</label>
                        <textarea id="print-data" class="form-control" rows="3" placeholder="Masukkan teks untuk dicetak..."></textarea>
                    </div>
                    <button id="print-btn" class="btn btn-primary">
                        <i class="bi bi-printer"></i> Kirim Perintah Cetak
                    </button>
                </div>
            </div>
        </div>
    </div>
    
    <div class="row">
        <div class="col-md-12">
            <div class="card">
                <div class="card-header">
                    <h5>Log Kejadian</h5>
                </div>
                <div class="card-body">
                    <div id="event-log" class="border p-3 bg-light" style="height: 200px; overflow-y: auto;">
                        <div class="text-secondary">Menunggu kejadian...</div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <script src="~/js/arduino.js"></script>
    <script>
        // Script tambahan untuk log kejadian
        $(function() {
            // Ini akan terhubung ke SignalR dalam implementasi sebenarnya
            // untuk log kejadian real-time
            
            // Untuk demo, kita akan menambahkan beberapa kejadian palsu
            setTimeout(() => addEventLog("Sistem diinisialisasi"), 1500);
            setTimeout(() => addEventLog("Arduino terhubung pada port /dev/ttyUSB0"), 3000);
        });
        
        function addEventLog(message) {
            const log = document.getElementById('event-log');
            if (!log) return;
            
            const time = new Date().toLocaleTimeString();
            const logEntry = document.createElement('div');
            logEntry.innerHTML = `<small class="text-muted">${time}</small> ${message}`;
            
            log.appendChild(logEntry);
            log.scrollTop = log.scrollHeight;
            
            // Batasi entri log
            while (log.children.length > 100) {
                log.removeChild(log.firstChild);
            }
        }
    </script>
}
```

```javascript:wwwroot/js/arduino.js
// JavaScript Kontrol Arduino
// File ini menangani interaksi client-side dengan kontroler Arduino

// Elemen DOM
let statusElement;
let openGateBtn;
let closeGateBtn;
let printBtn;
let printDataInput;
let statusInterval;

// Inisialisasi saat DOM telah sepenuhnya dimuat
document.addEventListener('DOMContentLoaded', function() {
    // Inisialisasi elemen DOM
    statusElement = document.getElementById('arduino-status');
    openGateBtn = document.getElementById('open-gate-btn');
    closeGateBtn = document.getElementById('close-gate-btn');
    printBtn = document.getElementById('print-btn');
    printDataInput = document.getElementById('print-data');

    // Tambahkan event listener
    if (openGateBtn) {
        openGateBtn.addEventListener('click', openGate);
    }
    
    if (closeGateBtn) {
        closeGateBtn.addEventListener('click', closeGate);
    }
    
    if (printBtn && printDataInput) {
        printBtn.addEventListener('click', sendPrintCommand);
    }

    // Mulai polling untuk status
    getStatus();
    statusInterval = setInterval(getStatus, 5000); // Polling setiap 5 detik
});

// Bersihkan saat meninggalkan halaman
window.addEventListener('beforeunload', function() {
    if (statusInterval) {
        clearInterval(statusInterval);
    }
});

// Dapatkan status Arduino
function getStatus() {
    fetch('/api/arduino/status')
        .then(response => {
            if (response.ok) {
                return response.json();
            }
            throw new Error('Gagal mendapatkan status Arduino');
        })
        .then(data => {
            if (statusElement) {
                statusElement.textContent = data.status || 'Tidak diketahui';
                statusElement.classList.remove('text-danger', 'text-success');
                
                if (data.status && data.status.includes('OPEN')) {
                    statusElement.classList.add('text-success');
                } else {
                    statusElement.classList.add('text-primary');
                }
            }
        })
        .catch(error => {
            console.error('Error:', error);
            if (statusElement) {
                statusElement.textContent = 'Kesalahan Koneksi';
                statusElement.classList.remove('text-success', 'text-primary');
                statusElement.classList.add('text-danger');
            }
        });
}

// Buka gerbang
function openGate() {
    if (openGateBtn) {
        openGateBtn.disabled = true;
    }
    
    fetch('/api/arduino/gate/open', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal membuka gerbang');
    })
    .then(data => {
        if (data.success) {
            showToast('Gerbang berhasil dibuka', 'success');
            // Update status segera
            getStatus();
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal membuka gerbang', 'danger');
    })
    .finally(() => {
        if (openGateBtn) {
            openGateBtn.disabled = false;
        }
    });
}

// Tutup gerbang
function closeGate() {
    if (closeGateBtn) {
        closeGateBtn.disabled = true;
    }
    
    fetch('/api/arduino/gate/close', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal menutup gerbang');
    })
    .then(data => {
        if (data.success) {
            showToast('Gerbang berhasil ditutup', 'success');
            // Update status segera
            getStatus();
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal menutup gerbang', 'danger');
    })
    .finally(() => {
        if (closeGateBtn) {
            closeGateBtn.disabled = false;
        }
    });
}

// Kirim perintah cetak
function sendPrintCommand() {
    if (!printDataInput || !printDataInput.value.trim()) {
        showToast('Silakan masukkan data untuk dicetak', 'warning');
        return;
    }
    
    if (printBtn) {
        printBtn.disabled = true;
    }
    
    const printData = printDataInput.value.trim();
    
    fetch('/api/arduino/print', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ printData: printData })
    })
    .then(response => {
        if (response.ok) {
            return response.json();
        }
        throw new Error('Gagal mengirim perintah cetak');
    })
    .then(data => {
        if (data.success) {
            showToast('Perintah cetak berhasil dikirim', 'success');
            if (printDataInput) {
                printDataInput.value = '';
            }
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Gagal mengirim perintah cetak', 'danger');
    })
    .finally(() => {
        if (printBtn) {
            printBtn.disabled = false;
        }
    });
}

// Tampilkan notifikasi toast
function showToast(message, type) {
    // Periksa apakah aplikasi memiliki kontainer toast
    let toastContainer = document.querySelector('.toast-container');
    
    if (!toastContainer) {
        // Buat kontainer toast jika belum ada
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Buat elemen toast
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type} border-0`;
    toast.id = toastId;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    // Buat konten toast
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
    // Tambahkan toast ke kontainer
    toastContainer.appendChild(toast);
    
    // Inisialisasi dan tampilkan toast
    const bsToast = new bootstrap.Toast(toast, {
        autohide: true,
        delay: 3000
    });
    bsToast.show();
    
    // Hapus toast dari DOM setelah disembunyikan
    toast.addEventListener('hidden.bs.toast', function() {
        toast.remove();
    });
}
```

```c#:Services/ArduinoService.cs
using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace ParkIRC.Services
{
    /// <summary>
    /// Pengaturan untuk koneksi Arduino
    /// </summary>
    public class ArduinoSettings
    {
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
    }

    /// <summary>
    /// Interface untuk layanan Arduino
    /// </summary>
    public interface IArduinoService
    {
        Task<bool> InitializeAsync();
        Task<bool> OpenGateAsync();
        Task<bool> CloseGateAsync();
        Task<string> GetStatusAsync();
        Task<bool> SendPrintCommandAsync(string printData);
        event EventHandler<string> ArduinoEventReceived;
    }

    /// <summary>
    /// Implementasi layanan Arduino untuk komunikasi dengan perangkat Arduino
    /// </summary>
    public class ArduinoService : IArduinoService, IDisposable
    {
        private readonly ILogger<ArduinoService> _logger;
        private readonly ArduinoSettings _settings;
        private SerialPort? _serialPort;
        private bool _isInitialized = false;
        private readonly StringBuilder _dataBuffer = new StringBuilder();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public event EventHandler<string>? ArduinoEventReceived;

        public ArduinoService(ILogger<ArduinoService> logger, IOptions<ArduinoSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// Menginisialisasi koneksi Arduino
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                await _semaphore.WaitAsync();
                
                if (_isInitialized)
                {
                    return true;
                }

                _logger.LogInformation("Mencoba menghubungkan ke Arduino pada port {PortName}", _settings.PortName);

                _serialPort = new SerialPort(
                    _settings.PortName,
                    _settings.BaudRate,
                    _settings.Parity,
                    _settings.DataBits,
                    _settings.StopBits
                );

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();

                // Tunggu sinyal siap dari Arduino
                _logger.LogInformation("Menunggu sinyal siap dari Arduino");
                var readyTask = WaitForResponseAsync("READY:", TimeSpan.FromSeconds(5));
                if (!await readyTask)
                {
                    _logger.LogError("Arduino tidak merespon dengan sinyal siap");
                    _serialPort.Close();
                    return false;
                }

                _isInitialized = true;
                _logger.LogInformation("Koneksi Arduino berhasil diinisialisasi pada port {PortName}", _settings.PortName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gagal menginisialisasi koneksi Arduino");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Membuka gerbang
        /// </summary>
        public async Task<bool> OpenGateAsync()
        {
            _logger.LogInformation("Mencoba membuka gerbang");
            return await SendCommandAsync("OPEN");
        }

        /// <summary>
        /// Menutup gerbang
        /// </summary>
        public async Task<bool> CloseGateAsync()
        {
            _logger.LogInformation("Mencoba menutup gerbang");
            return await SendCommandAsync("CLOSE");
        }

        /// <summary>
        /// Mendapatkan status Arduino dan gerbang
        /// </summary>
        public async Task<string> GetStatusAsync()
        {
            if (!await EnsureInitializedAsync())
            {
                return string.Empty;
            }

            try
            {
                await _semaphore.WaitAsync();
                
                _dataBuffer.Clear();
                _serialPort!.WriteLine("STATUS");
                
                _logger.LogInformation("Menunggu respons status dari Arduino");
                var response = await ReadLineWithTimeoutAsync(TimeSpan.FromSeconds(3));
                if (string.IsNullOrEmpty(response))
                {
                    _logger.LogWarning("Tidak ada respons status diterima dari Arduino");
                    return string.Empty;
                }

                _logger.LogInformation("Status Arduino: {Status}", response);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mendapatkan status Arduino");
                return string.Empty;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Mengirim perintah cetak ke Arduino
        /// </summary>
        public async Task<bool> SendPrintCommandAsync(string printData)
        {
            _logger.LogInformation("Mengirim perintah cetak dengan data: {PrintDataLength} karakter", printData.Length);
            return await SendCommandAsync($"PRINT:{printData}");
        }

        /// <summary>
        /// Mengirim perintah ke Arduino dan menunggu respons
        /// </summary>
        private async Task<bool> SendCommandAsync(string command)
        {
            if (!await EnsureInitializedAsync())
            {
                return false;
            }

            try
            {
                await _semaphore.WaitAsync();
                
                _dataBuffer.Clear();
                _serialPort!.WriteLine(command);
                
                _logger.LogInformation("Perintah dikirim: {Command}, menunggu respons", command);
                
                var response = await ReadLineWithTimeoutAsync(TimeSpan.FromSeconds(3));
                
                var result = response != null && response.StartsWith("OK:");
                _logger.LogInformation("Respons diterima: {Response}, hasil: {Result}", response, result ? "Berhasil" : "Gagal");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat mengirim perintah {Command} ke Arduino", command);
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Memastikan koneksi Arduino sudah diinisialisasi
        /// </summary>
        private async Task<bool> EnsureInitializedAsync()
        {
            if (!_isInitialized)
            {
                _logger.LogInformation("Koneksi Arduino belum diinisialisasi, mencoba inisialisasi");
                return await InitializeAsync();
            }
            return _isInitialized;
        }

        /// <summary>
        /// Menunggu respons yang diawali dengan prefix tertentu dari Arduino
        /// </summary>
        private async Task<bool> WaitForResponseAsync(string expectedPrefix, TimeSpan timeout)
        {
            var startTime = DateTime.Now;
            
            while (DateTime.Now - startTime < timeout)
            {
                var response = await ReadLineWithTimeoutAsync(TimeSpan.FromMilliseconds(500));
                
                if (!string.IsNullOrEmpty(response) && response.StartsWith(expectedPrefix))
                {
                    _logger.LogInformation("Respons yang diharapkan diterima: {Response}", response);
                    return true;
                }
                
                await Task.Delay(100);
            }
            
            _logger.LogWarning("Timeout menunggu respons dengan awalan: {ExpectedPrefix}", expectedPrefix);
            return false;
        }

        /// <summary>
        /// Membaca baris data dari buffer dengan timeout
        /// </summary>
        private async Task<string> ReadLineWithTimeoutAsync(TimeSpan timeout)
        {
            var startTime = DateTime.Now;
            
            while (DateTime.Now - startTime < timeout)
            {
                if (_dataBuffer.Length > 0 && _dataBuffer.ToString().Contains('\n'))
                {
                    var bufferStr = _dataBuffer.ToString();
                    var lineEnd = bufferStr.IndexOf('\n');
                    
                    if (lineEnd >= 0)
                    {
                        var line = bufferStr.Substring(0, lineEnd).Trim();
                        _dataBuffer.Remove(0, lineEnd + 1);
                        return line;
                    }
                }
                
                await Task.Delay(10);
            }
            
            return string.Empty;
        }

        /// <summary>
        /// Handler event data diterima dari port serial
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return;

            try
            {
                string data = _serialPort.ReadExisting();
                _dataBuffer.Append(data);

                // Periksa notifikasi event dari Arduino
                var bufferStr = _dataBuffer.ToString();
                var lines = bufferStr.Split('\n');
                
                if (lines.Length > 1)
                {
                    for (int i = 0; i < lines.Length - 1; i++)
                    {
                        var line = lines[i].Trim();
                        
                        if (line.StartsWith("EVENT:"))
                        {
                            _logger.LogInformation("Event diterima dari Arduino: {Event}", line);
                            OnArduinoEvent(line);
                        }
                    }
                    
                    // Simpan baris yang belum lengkap di buffer
                    _dataBuffer.Clear();
                    _dataBuffer.Append(lines[lines.Length - 1]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saat membaca data dari Arduino");
            }
        }

        /// <summary>
        /// Memicu event ketika ada notifikasi dari Arduino
        /// </summary>
        private void OnArduinoEvent(string eventData)
        {
            ArduinoEventReceived?.Invoke(this, eventData);
        }

        /// <summary>
        /// Membersihkan resource
        /// </summary>
        public void Dispose()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    _logger.LogInformation("Menutup koneksi serial port Arduino");
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saat menutup port serial Arduino");
                }
            }
            
            _semaphore.Dispose();
        }
    }
}
```
Untuk mengkompilasi dan menjalankan aplikasi Arduino pada .NET, ikuti langkah-langkah berikut:

1. **Persiapan Lingkungan**
   - Pastikan .NET SDK (minimal versi 6.0) sudah terpasang di sistem Anda:
   ```bash
   sudo apt-get update
   sudo apt-get install -y dotnet-sdk-6.0
   ```

   - Pasang paket pendukung yang diperlukan:
   ```bash
   sudo apt-get install -y libgdiplus libc6-dev
   ```

   - Tambahkan pengguna ke grup dialout agar bisa mengakses port serial:
   ```bash
   sudo usermod -a -G dialout $USER
   sudo chmod a+rw /dev/ttyUSB0  # Sesuaikan dengan port Arduino Anda
   ```

2. **Kompilasi Aplikasi**
   - Masuk ke direktori proyek:
   ```bash
   cd /home/tekno/Desktop/AAA/parkir28Mar
   ```
   
   - Kompilasi proyek:
   ```bash
   dotnet build
   ```

3. **Jalankan Aplikasi**
   - Mode pengembangan:
   ```bash
   dotnet run
   ```
   
   - Atau build versi rilis:
   ```bash
   dotnet publish -c Release -o ./publish
   cd ./publish
   dotnet ParkIRC.dll
   ```

4. **Akses Aplikasi**
   - Buka browser dan akses: http://localhost:5050/Arduino
   - Pastikan Arduino sudah terhubung ke port yang dikonfigurasi di appsettings.json

5. **Konfigurasi Arduino**
   - Pastikan file sketch Arduino (`GateController.ino`) sudah diupload ke board Arduino
   - Periksa port serial di `appsettings.json` (biasanya `/dev/ttyUSB0` atau `/dev/ttyACM0`)

Integrasi Arduino ini telah dilengkapi dengan dukungan bahasa Indonesia dan peningkatan error handling untuk memudahkan penggunaan dan pemeliharaan sistem parkir Anda.
