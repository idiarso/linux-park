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
