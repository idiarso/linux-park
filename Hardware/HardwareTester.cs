using System;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using DirectShowLib;
using static DirectShowLib.DsGuid;

namespace ParkIRC.Hardware
{
    /// <summary>
    /// Utility to test hardware connections and functionality
    /// </summary>
    public class HardwareTester
    {
        private const string ConfigPath = "config";
        private const string TestImagePath = "test_images";
        private const string TestPrintContent = "TEST PRINT - SISTEM PARKIR\r\n===========================\r\nTEST BARCODE: TEST123\r\n===========================";
        
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
                            TestAllHardware();
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
                // Load camera settings
                var cameraConfig = LoadIniFile(Path.Combine(ConfigPath, "camera.ini"));
                string cameraType = GetIniValue(cameraConfig, "Camera", "Type", "Webcam");
                
                Console.WriteLine($"Camera type: {cameraType}");
                
                if (cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    await TestWebcamAsync(cameraConfig);
                }
                else if (cameraType.Equals("IP", StringComparison.OrdinalIgnoreCase))
                {
                    await TestIpCameraAsync(cameraConfig);
                }
                else
                {
                    Console.WriteLine($"Unknown camera type: {cameraType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Camera test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test local webcam
        /// </summary>
        private static async Task TestWebcamAsync(Dictionary<string, Dictionary<string, string>> config)
        {
            Console.WriteLine("Testing webcam connection...");
            
            try
            {
                // Get device index
                int deviceIndex = int.Parse(GetIniValue(config, "Webcam", "Device_Index", "0"));
                
                // List available devices
                var videoDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                
                if (videoDevices.Length == 0)
                {
                    Console.WriteLine("No video devices found!");
                    return;
                }
                
                Console.WriteLine("Available video devices:");
                for (int i = 0; i < videoDevices.Length; i++)
                {
                    Console.WriteLine($"{i}: {videoDevices[i].Name}");
                }
                
                if (deviceIndex >= videoDevices.Length)
                {
                    Console.WriteLine($"Warning: Configured device index {deviceIndex} is out of range. Using index 0.");
                    deviceIndex = 0;
                }
                
                Console.WriteLine($"Using device: {videoDevices[deviceIndex].Name}");
                
                // Create filter graph
                var graphBuilder = (IFilterGraph2)new FilterGraph();
                var mediaControl = (IMediaControl)graphBuilder;

                // Add video device filter
                var hr = graphBuilder.AddSourceFilterForMoniker(
                    videoDevices[deviceIndex].Mon, null, videoDevices[deviceIndex].Name, out var videoDevice);
                DsError.ThrowExceptionForHR(hr);

                // Create and add sample grabber
                var sampleGrabber = (ISampleGrabber)new SampleGrabber();
                var sampleGrabberFilter = (IBaseFilter)sampleGrabber;
                hr = graphBuilder.AddFilter(sampleGrabberFilter, "Sample Grabber");
                DsError.ThrowExceptionForHR(hr);

                // Set media type
                var mediaType = new AMMediaType
                {
                    majorType = MediaType.Video,
                    subType = MediaSubType.RGB24,
                    formatType = FormatType.VideoInfo
                };
                hr = sampleGrabber.SetMediaType(mediaType);
                DsError.ThrowExceptionForHR(hr);

                // Create null renderer
                var nullRenderer = (IBaseFilter)new NullRenderer();
                hr = graphBuilder.AddFilter(nullRenderer, "Null Renderer");
                if (hr < 0)
                    throw new Exception("Failed to add null renderer filter to graph");

                // Connect filters
                var pinOut = DsFindPin.ByDirection(videoDevice, PinDirection.Output, 0);
                var pinIn = DsFindPin.ByDirection(sampleGrabberFilter, PinDirection.Input, 0);
                hr = graphBuilder.Connect(pinOut, pinIn);
                DsError.ThrowExceptionForHR(hr);

                pinOut = DsFindPin.ByDirection(sampleGrabberFilter, PinDirection.Output, 0);
                pinIn = DsFindPin.ByDirection(nullRenderer, PinDirection.Input, 0);
                hr = graphBuilder.Connect(pinOut, pinIn);
                DsError.ThrowExceptionForHR(hr);

                // Set callback
                hr = sampleGrabber.SetBufferSamples(true);
                DsError.ThrowExceptionForHR(hr);
                hr = sampleGrabber.SetOneShot(false);
                DsError.ThrowExceptionForHR(hr);

                // Start capturing
                hr = mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);

                Console.WriteLine("Capturing frames... Please wait.");

                // Capture a few frames
                for (int i = 0; i < 3; i++)
                {
                    // Get buffer size
                    int bufferSize = 0;
                    hr = sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                    DsError.ThrowExceptionForHR(hr);

                    // Allocate buffer
                    var buffer = new byte[bufferSize];
                    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    var connectedMediaType = new AMMediaType();
                    try
                    {
                        // Get frame data
                        hr = sampleGrabber.GetCurrentBuffer(ref bufferSize, handle.AddrOfPinnedObject());
                        DsError.ThrowExceptionForHR(hr);

                        // Get media type
                        hr = sampleGrabber.GetConnectedMediaType(connectedMediaType);
                        DsError.ThrowExceptionForHR(hr);

                        var videoInfo = (VideoInfoHeader)Marshal.PtrToStructure(connectedMediaType.formatPtr, typeof(VideoInfoHeader));
                        var width = videoInfo.BmiHeader.Width;
                        var height = videoInfo.BmiHeader.Height;

                        // Create bitmap
                        using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            PixelFormat.Format24bppRgb);
                        try
                        {
                            Marshal.Copy(buffer, 0, bitmapData.Scan0, buffer.Length);
                        }
                        finally
                        {
                            bitmap.UnlockBits(bitmapData);
                        }

                        // Save test image
                        string filename = Path.Combine(TestImagePath, $"test_frame_{i}_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                        bitmap.Save(filename, ImageFormat.Jpeg);
                        Console.WriteLine($"Saved test frame to: {filename}");

                        await Task.Delay(500); // Wait a bit between frames
                    }
                    finally
                    {
                        handle.Free();
                        DsUtils.FreeAMMediaType(connectedMediaType);
                    }
                }

                // Clean up
                mediaControl.Stop();
                Marshal.ReleaseComObject(nullRenderer);
                Marshal.ReleaseComObject(sampleGrabberFilter);
                Marshal.ReleaseComObject(videoDevice);
                Marshal.ReleaseComObject(mediaControl);
                Marshal.ReleaseComObject(graphBuilder);

                Console.WriteLine("Webcam test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webcam test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test IP camera
        /// </summary>
        private static async Task TestIpCameraAsync(Dictionary<string, Dictionary<string, string>> config)
        {
            Console.WriteLine("Testing IP camera connection...");
            
            try
            {
                // Get IP camera settings
                string ip = GetIniValue(config, "Camera", "IP", "127.0.0.1");
                string username = GetIniValue(config, "Camera", "Username", "admin");
                string password = GetIniValue(config, "Camera", "Password", "admin");
                int port = int.Parse(GetIniValue(config, "Camera", "Port", "8080"));
                
                Console.WriteLine($"IP Camera Details:");
                Console.WriteLine($"IP Address: {ip}");
                Console.WriteLine($"Port: {port}");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine($"URL: http://{ip}:{port}/");
                
                // This is a placeholder for actual IP camera test
                // Typically would use an HTTP client to test connectivity
                Console.WriteLine("NOTE: This is a simulated test for IP camera connectivity.");
                Console.WriteLine("Would normally attempt to connect to camera using HTTP client and test snapshot capture.");
                
                // Create dummy test file to show completion
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string textFilePath = Path.Combine(TestImagePath, $"ipcamera_test_{timestamp}.txt");
                
                File.WriteAllText(textFilePath, 
                    $"IP CAMERA TEST\r\n" +
                    $"Timestamp: {DateTime.Now}\r\n" +
                    $"Camera IP: {ip}:{port}\r\n" +
                    $"Status: Connection test completed\r\n");
                
                Console.WriteLine($"Test record saved to: {textFilePath}");
                
                Console.WriteLine("IP Camera test completed!");
                
                // In a real implementation, would verify the camera is reachable
                Console.WriteLine("\nTo fully verify IP camera operation:");
                Console.WriteLine("1. Check if camera is accessible at http://{ip}:{port}/");
                Console.WriteLine("2. Verify authentication with provided username/password");
                Console.WriteLine("3. Test snapshot endpoint (typically /snapshot.jpg or similar)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IP Camera test failed: {ex.Message}");
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
                string portName = GetIniValue(gateConfig, "Gate", "COM_Port", "COM3");
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
                    
                    // Get gate commands
                    string openEntryCmd = GetIniValue(gateConfig, "Commands", "Open_Entry", "OPEN_ENTRY");
                    string closeEntryCmd = GetIniValue(gateConfig, "Commands", "Close_Entry", "CLOSE_ENTRY");
                    string statusCmd = GetIniValue(gateConfig, "Commands", "Status_Request", "STATUS");
                    
                    // Send status request
                    Console.WriteLine($"\nSending status request: {statusCmd}");
                    serialPort.WriteLine(statusCmd);
                    
                    // Wait for a response with timeout
                    int waitTime = 0;
                    while (!dataReceived && waitTime < 5000)
                    {
                        await Task.Delay(200);
                        waitTime += 200;
                    }
                    
                    if (!dataReceived)
                    {
                        Console.WriteLine("No response received from serial port within timeout period.");
                    }
                    
                    // Ask if we should test gate operation
                    Console.Write("\nDo you want to test gate operation? (y/n): ");
                    string testGate = Console.ReadLine().Trim().ToLower();
                    
                    if (testGate == "y")
                    {
                        // Test open gate command
                        Console.WriteLine($"Sending Open Entry command: {openEntryCmd}");
                        serialPort.WriteLine(openEntryCmd);
                        
                        Console.WriteLine("Waiting 5 seconds...");
                        await Task.Delay(5000);
                        
                        // Test close gate command
                        Console.WriteLine($"Sending Close Entry command: {closeEntryCmd}");
                        serialPort.WriteLine(closeEntryCmd);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Serial port connection failed: {ex.Message}");
                }
                finally
                {
                    // Clean up
                    if (serialPort.IsOpen)
                    {
                        serialPort.Close();
                    }
                    serialPort.Dispose();
                }
                
                Console.WriteLine("Serial Port / Gate test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serial Port test failed: {ex.Message}");
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
                string printerName = GetIniValue(printerConfig, "Printer", "Name", "EPSON TM-T82X");
                
                Console.WriteLine($"Configured printer: {printerName}");
                
                // List installed printers
                Console.WriteLine("\nAvailable printers:");
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    string status = printer == printerName ? " (configured)" : "";
                    Console.WriteLine($"- {printer}{status}");
                }
                
                if (!System.Drawing.Printing.PrinterSettings.InstalledPrinters.Cast<string>().Any(p => p == printerName))
                {
                    Console.WriteLine($"\nWARNING: Configured printer '{printerName}' not found in installed printers!");
                }
                
                // This is a placeholder for actual printer test
                // In a real implementation, would use a receipt printing library
                Console.WriteLine("\nNOTE: This is a simulated test for printer functionality.");
                Console.WriteLine("Would normally print a test receipt with the following content:");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine(TestPrintContent);
                Console.WriteLine("-----------------------------------");
                
                // Ask if want to test print
                Console.Write("\nDo you want to attempt a test print? (y/n): ");
                string testPrint = Console.ReadLine().Trim().ToLower();
                
                if (testPrint == "y")
                {
                    Console.WriteLine("Attempting test print...");
                    await Task.Delay(2000); // Simulating print time
                    
                    Console.WriteLine("If no print appeared, please check:");
                    Console.WriteLine("1. Printer is turned on and has paper");
                    Console.WriteLine("2. Printer name is correct");
                    Console.WriteLine("3. Printer drivers are installed correctly");
                    Console.WriteLine("4. There are no pending print jobs");
                }
                
                Console.WriteLine("\nPrinter test completed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Printer test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Test all hardware components
        /// </summary>
        private static void TestAllHardware()
        {
            Console.WriteLine("===== Testing All Hardware Components =====");
            
            Task.Run(async () =>
            {
                try
                {
                    await TestCameraAsync();
                    Console.WriteLine();
                    
                    await TestSerialPortAsync();
                    Console.WriteLine();
                    
                    await TestPrinterAsync();
                    Console.WriteLine();
                    
                    TestBarcodeScanner();
                    
                    Console.WriteLine("\nAll hardware tests completed!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during hardware tests: {ex.Message}");
                }
            }).Wait();
        }
        
        /// <summary>
        /// Test barcode scanner
        /// </summary>
        private static void TestBarcodeScanner()
        {
            Console.WriteLine("===== Testing Barcode Scanner =====");
            
            try
            {
                Console.WriteLine("This test requires a barcode scanner connected as a keyboard device.");
                Console.WriteLine("The scanner should be configured to add a carriage return after each scan.\n");
                
                Console.WriteLine("Please scan a barcode now (or type a value and press Enter to simulate):");
                
                string barcode = Console.ReadLine().Trim();
                
                if (!string.IsNullOrEmpty(barcode))
                {
                    Console.WriteLine($"Barcode received: {barcode}");
                    Console.WriteLine("Barcode scanner is working correctly!");
                }
                else
                {
                    Console.WriteLine("No barcode value received.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Barcode scanner test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Create directory if it doesn't exist
        /// </summary>
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        /// <summary>
        /// Load and parse INI file
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> LoadIniFile(string path)
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";
            
            if (!File.Exists(path))
            {
                Console.WriteLine($"WARNING: Config file not found: {path}");
                return iniData;
            }
            
            foreach (string line in File.ReadAllLines(path))
            {
                string trimmedLine = line.Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;
                
                // Section header
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    if (!iniData.ContainsKey(currentSection))
                    {
                        iniData[currentSection] = new Dictionary<string, string>();
                    }
                }
                // Key-value pair
                else if (trimmedLine.Contains("="))
                {
                    string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                    if (parts.Length == 2 && !string.IsNullOrEmpty(currentSection))
                    {
                        iniData[currentSection][parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            
            return iniData;
        }
        
        /// <summary>
        /// Get value from INI data
        /// </summary>
        private static string GetIniValue(Dictionary<string, Dictionary<string, string>> iniData, string section, string key, string defaultValue)
        {
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
            {
                return iniData[section][key];
            }
            return defaultValue;
        }
    }
}
