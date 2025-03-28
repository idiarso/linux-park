using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace ParkIRC.Hardware
{
    /// <summary>
    /// Utility to test hardware connections and functionality
    /// </summary>
    public class HardwareTester : IDisposable
    {
        private const string ConfigPath = "config";
        private const string TestImagePath = "test_images";
        private const string TestPrintContent = "TEST PRINT - SISTEM PARKIR\r\n===========================\r\nTEST BARCODE: TEST123\r\n===========================";
        
        private readonly ILogger<HardwareTester> _logger;
        private VideoCapture? _videoCapture;

        public HardwareTester(ILogger<HardwareTester> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Entry point for hardware test utility
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("===== Hardware Test Utility =====");
            Console.WriteLine("This tool tests connections to cameras, gates, and printers");
            Console.WriteLine();
            
            try
            {
                // Ensure test directory exists
                EnsureDirectoryExists(TestImagePath);
                
                DisplayMenu();
                
                bool exit = false;
                while (!exit)
                {
                    Console.Write("Enter option (or 'q' to quit): ");
                    string option = Console.ReadLine().Trim().ToLower();
                    
                    switch (option)
                    {
                        case "1":
                            await TestCameraAsync();
                            break;
                        case "2":
                            await TestSerialPortAsync();
                            break;
                        case "3":
                            await TestPrinterAsync();
                            break;
                        case "4":
                            await TestAllHardware();
                            break;
                        case "5":
                            TestBarcodeScanner();
                            break;
                        case "q":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                    
                    if (!exit)
                    {
                        Console.WriteLine();
                        DisplayMenu();
                    }
                }
                
                Console.WriteLine("Hardware test utility closing...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        /// <summary>
        /// Display menu options
        /// </summary>
        private static void DisplayMenu()
        {
            Console.WriteLine("Available Test Options:");
            Console.WriteLine("1. Test Camera (Webcam/IP)");
            Console.WriteLine("2. Test Serial Port / Gate");
            Console.WriteLine("3. Test Thermal Printer");
            Console.WriteLine("4. Test All Hardware");
            Console.WriteLine("5. Test Barcode Scanner");
            Console.WriteLine("q. Quit");
        }
        
        /// <summary>
        /// Test camera connection and capture
        /// </summary>
        private static async Task TestCameraAsync()
        {
            Console.WriteLine("===== Testing Camera =====");
            
            try
            {
                var hardwareTester = new HardwareTester(null);
                if (await hardwareTester.TestCameraConnectionAsync())
                {
                    Console.WriteLine("Camera test completed successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Camera test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test serial port connection
        /// </summary>
        private static async Task TestSerialPortAsync()
        {
            Console.WriteLine("===== Testing Serial Port / Gate =====");
            
            try
            {
                // Load gate settings
                var gateConfig = LoadIniFile(Path.Combine(ConfigPath, "gate.ini"));
                string portName = GetIniValue(gateConfig, "Gate", "COM_Port", "/dev/ttyUSB0");
                int baudRate = int.Parse(GetIniValue(gateConfig, "Gate", "Baud_Rate", "9600"));
                
                Console.WriteLine($"Serial Port Configuration:");
                Console.WriteLine($"Port: {portName}");
                Console.WriteLine($"Baud Rate: {baudRate}");
                
                // List available ports
                string[] availablePorts = SerialPort.GetPortNames();
                Console.WriteLine("\nAvailable COM ports:");
                foreach (string port in availablePorts)
                {
                    Console.WriteLine($"- {port}");
                }
                
                if (Array.IndexOf(availablePorts, portName) < 0)
                {
                    Console.WriteLine($"\nWARNING: Configured port {portName} not found in available ports!");
                    Console.Write("Do you want to continue testing anyway? (y/n): ");
                    string answer = Console.ReadLine().Trim().ToLower();
                    
                    if (answer != "y")
                    {
                        Console.WriteLine("Serial port test aborted.");
                        return;
                    }
                }
                
                // Test opening serial port
                Console.WriteLine($"\nTesting connection to {portName}...");
                
                SerialPort serialPort = new SerialPort(portName, baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                
                // Add data received handler
                bool dataReceived = false;
                serialPort.DataReceived += (sender, e) =>
                {
                    try
                    {
                        string data = serialPort.ReadExisting();
                        Console.WriteLine($"Data received: {data}");
                        dataReceived = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading data: {ex.Message}");
                    }
                };
                
                try
                {
                    serialPort.Open();
                    Console.WriteLine($"Successfully opened port {portName}");
                    
                    // Send test command
                    string testCommand = "TEST\r\n";
                    Console.WriteLine($"Sending test command: {testCommand}");
                    serialPort.Write(testCommand);
                    
                    // Wait for response
                    await Task.Delay(1000);
                    
                    if (!dataReceived)
                    {
                        Console.WriteLine("No response received from device");
                    }
                    else
                    {
                        Console.WriteLine("Device responded successfully!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error testing serial port: {ex.Message}");
                }
                finally
                {
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial port test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test thermal printer
        /// </summary>
        private static async Task TestPrinterAsync()
        {
            Console.WriteLine("===== Testing Thermal Printer =====");
            
            try
            {
                // Load printer settings
                var printerConfig = LoadIniFile(Path.Combine(ConfigPath, "printer.ini"));
                string portName = GetIniValue(printerConfig, "Printer", "COM_Port", "/dev/ttyUSB0");
                int baudRate = int.Parse(GetIniValue(printerConfig, "Printer", "Baud_Rate", "9600"));
                
                Console.WriteLine($"Printer Configuration:");
                Console.WriteLine($"Port: {portName}");
                Console.WriteLine($"Baud Rate: {baudRate}");
                
                SerialPort printer = new SerialPort(portName, baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                
                try
                {
                    printer.Open();
                    Console.WriteLine($"Successfully opened printer port {portName}");
                    
                    // Send test print command
                    Console.WriteLine($"Sending test print command...");
                    printer.Write(TestPrintContent + "\r\n");
                    
                    // Wait a moment for printing
                    await Task.Delay(2000);
                    
                    Console.WriteLine("Test print command sent successfully!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error testing printer: {ex.Message}");
                }
                finally
                {
                    if (printer.IsOpen)
                    {
                        printer.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Printer test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test barcode scanner
        /// </summary>
        private static void TestBarcodeScanner()
        {
            Console.WriteLine("===== Testing Barcode Scanner =====");
            
            try
            {
                // Load scanner settings
                var scannerConfig = LoadIniFile(Path.Combine(ConfigPath, "scanner.ini"));
                string portName = GetIniValue(scannerConfig, "Scanner", "COM_Port", "/dev/ttyUSB0");
                int baudRate = int.Parse(GetIniValue(scannerConfig, "Scanner", "Baud_Rate", "9600"));
                
                Console.WriteLine($"Scanner Configuration:");
                Console.WriteLine($"Port: {portName}");
                Console.WriteLine($"Baud Rate: {baudRate}");
                
                SerialPort scanner = new SerialPort(portName, baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                
                try
                {
                    scanner.Open();
                    Console.WriteLine($"Successfully opened scanner port {portName}");
                    
                    // Wait for barcode scan
                    Console.WriteLine("Waiting for barcode scan (press Ctrl+C to cancel)...");
                    string barcode = scanner.ReadLine();
                    Console.WriteLine($"Scanned barcode: {barcode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error testing barcode scanner: {ex.Message}");
                }
                finally
                {
                    if (scanner.IsOpen)
                    {
                        scanner.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scanner test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test all hardware components
        /// </summary>
        private static async Task TestAllHardware()
        {
            Console.WriteLine("===== Testing All Hardware =====");
            
            try
            {
                // Test camera
                Console.WriteLine("\nTesting Camera...");
                var hardwareTester = new HardwareTester(null);
                if (!await hardwareTester.TestCameraConnectionAsync())
                {
                    Console.WriteLine("Camera test failed!");
                }
                
                // Test serial port
                Console.WriteLine("\nTesting Serial Port...");
                await TestSerialPortAsync();
                
                // Test printer
                Console.WriteLine("\nTesting Printer...");
                await TestPrinterAsync();
                
                // Test barcode scanner
                Console.WriteLine("\nTesting Barcode Scanner...");
                TestBarcodeScanner();
                
                Console.WriteLine("\nAll hardware tests completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing hardware: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Ensure directory exists
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// Load INI file into dictionary
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> LoadIniFile(string filePath)
        {
            var config = new Dictionary<string, Dictionary<string, string>>();
            
            if (File.Exists(filePath))
            {
                string[] lines = File.ReadAllLines(filePath);
                string? currentSection = null;
                
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    
                    if (trimmed.StartsWith("["))
                    {
                        currentSection = trimmed.Trim('[', ']');
                        if (!config.ContainsKey(currentSection))
                        {
                            config[currentSection] = new Dictionary<string, string>();
                        }
                    }
                    else if (!string.IsNullOrEmpty(trimmed) && currentSection != null)
                    {
                        int equalsIndex = trimmed.IndexOf('=');
                        if (equalsIndex > 0)
                        {
                            string key = trimmed.Substring(0, equalsIndex).Trim();
                            string value = trimmed.Substring(equalsIndex + 1).Trim();
                            config[currentSection][key] = value;
                        }
                    }
                }
            }
            
            return config;
        }
        
        /// <summary>
        /// Get value from INI configuration
        /// </summary>
        private static string GetIniValue(Dictionary<string, Dictionary<string, string>> config, string section, string key, string defaultValue)
        {
            if (config.TryGetValue(section, out var sectionValues) &&
                sectionValues.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }
        
        public async Task<bool> TestCameraConnectionAsync(int deviceIndex = 0)
        {
            try
            {
                _videoCapture = new VideoCapture(deviceIndex);
                if (!_videoCapture.IsOpened())
                {
                    _logger.LogError("Failed to open camera device {DeviceIndex}", deviceIndex);
                    return false;
                }

                // Capture a frame to test
                using var mat = new Mat();
                if (!_videoCapture.Read(mat))
                {
                    _logger.LogError("Failed to capture frame from camera");
                    return false;
                }

                _logger.LogInformation("Camera connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing camera connection");
                return false;
            }
            finally
            {
                _videoCapture?.Dispose();
            }
        }

        public async Task<string> CaptureTestImageAsync(int deviceIndex = 0)
        {
            try
            {
                if (_videoCapture == null || !_videoCapture.IsOpened())
                {
                    _videoCapture = new VideoCapture(deviceIndex);
                    if (!_videoCapture.IsOpened())
                    {
                        throw new Exception($"Failed to open camera device {deviceIndex}");
                    }
                }

                using (var mat = new Mat())
                {
                    if (!_videoCapture.Read(mat))
                    {
                        throw new Exception("Failed to capture frame from camera");
                    }

                    // Convert Mat to byte array
                    byte[] imageData = mat.ToBytes();

                    // Create filename with timestamp
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"test_capture_{timestamp}.jpg";
                    string fullPath = Path.Combine(TestImagePath, filename);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                    // Create and save image using ImageSharp
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageData);
                    using var stream = File.Create(fullPath);
                    image.Save(stream, new JpegEncoder());

                    _logger.LogInformation($"Test image captured and saved to {fullPath}");
                    return fullPath;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing test image");
                throw;
            }
        }

        public void Dispose()
        {
            _videoCapture?.Dispose();
        }
    }
}
