using System;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.InteropServices;
using DirectShowLib;
using Npgsql;
using static DirectShowLib.DsGuid;

namespace ParkIRC.Hardware
{
    /// <summary>
    /// Manages communication with hardware devices:
    /// - Camera (IP camera or webcam)
    /// - Gate controller (via serial port)
    /// - Thermal printer
    /// </summary>
    public class HardwareManager : IDisposable
    {
        private SerialPort _serialPort;
        private IFilterGraph2 _graphBuilder;
        private IMediaControl _mediaControl;
        private IBaseFilter _videoDevice;
        private ISampleGrabber _sampleGrabber;
        private bool _isInitialized = false;
        private string _basePath;
        private bool _disposed = false;
        private static readonly object _lock = new object();
        private static HardwareManager _instance;

        // Configuration values
        private string _cameraType;
        private string _cameraIp;
        private string _cameraUsername;
        private string _cameraPassword;
        private int _cameraPort;
        private string _cameraResolution;
        private int _webcamDeviceIndex;
        private string _comPort;
        private int _baudRate;
        private string _imageBasePath;
        private string _entrySubfolder;
        private string _exitSubfolder;
        private string _filenameFormat;

        // Camera event delegate
        public delegate void ImageCapturedEventHandler(object sender, ImageCapturedEventArgs e);
        public event ImageCapturedEventHandler ImageCaptured;

        // Serial event delegate
        public delegate void CommandReceivedEventHandler(object sender, CommandReceivedEventArgs e);
        public event CommandReceivedEventHandler CommandReceived;

        // Singleton implementation
        public static HardwareManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new HardwareManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private HardwareManager()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Initializes hardware connections
        /// </summary>
        public bool Initialize()
        {
            try
            {
                if (_isInitialized)
                    return true;

                InitializeSerialPort();
                InitializeCamera();
                EnsureDirectoriesExist();
                
                _isInitialized = true;
                Console.WriteLine("Hardware manager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize hardware: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads configuration from INI files
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Load camera settings
                string cameraConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "camera.ini");
                var cameraConfig = ParseIniFile(cameraConfigPath);
                
                _cameraType = GetIniValue(cameraConfig, "Camera", "Type", "Webcam");
                _cameraIp = GetIniValue(cameraConfig, "Camera", "IP", "127.0.0.1");
                _cameraUsername = GetIniValue(cameraConfig, "Camera", "Username", "admin");
                _cameraPassword = GetIniValue(cameraConfig, "Camera", "Password", "admin");
                _cameraPort = int.Parse(GetIniValue(cameraConfig, "Camera", "Port", "8080"));
                _cameraResolution = GetIniValue(cameraConfig, "Camera", "Resolution", "640x480");
                _webcamDeviceIndex = int.Parse(GetIniValue(cameraConfig, "Webcam", "Device_Index", "0"));

                _imageBasePath = GetIniValue(cameraConfig, "Storage", "Base_Path", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images"));
                _entrySubfolder = GetIniValue(cameraConfig, "Storage", "Entry_Subfolder", "Entry");
                _exitSubfolder = GetIniValue(cameraConfig, "Storage", "Exit_Subfolder", "Exit");
                _filenameFormat = GetIniValue(cameraConfig, "Storage", "Filename_Format", "{0}_{1:yyyyMMdd_HHmmss}");

                // Load gate settings
                string gateConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "gate.ini");
                var gateConfig = ParseIniFile(gateConfigPath);
                
                _comPort = GetIniValue(gateConfig, "Gate", "COM_Port", "COM1");
                _baudRate = int.Parse(GetIniValue(gateConfig, "Gate", "Baud_Rate", "9600"));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes serial port for communication with gate controller
        /// </summary>
        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort(_comPort, _baudRate)
                {
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                Console.WriteLine($"Connected to serial port {_comPort}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize serial port: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Initializes camera device (webcam or IP camera)
        /// </summary>
        private async Task InitializeCamera()
        {
            try
            {
                _graphBuilder = (IFilterGraph2)new FilterGraph();
                _mediaControl = (IMediaControl)_graphBuilder;

                // Create and add sample grabber
                _sampleGrabber = (ISampleGrabber)new SampleGrabber();
                var sampleGrabberFilter = (IBaseFilter)_sampleGrabber;
                var hr = _graphBuilder.AddFilter(sampleGrabberFilter, "Sample Grabber");
                DsError.ThrowExceptionForHR(hr);

                // Get first video device
                _videoDevice = await GetFirstVideoCaptureDeviceAsync();
                if (_videoDevice == null)
                {
                    throw new Exception("No video capture device found");
                }

                hr = _graphBuilder.AddFilter(_videoDevice, "Video Capture Device");
                DsError.ThrowExceptionForHR(hr);

                // Set media type
                var mediaType = new AMMediaType
                {
                    majorType = MediaType.Video,
                    subType = MediaSubType.RGB24,
                    formatType = FormatType.VideoInfo
                };
                hr = _sampleGrabber.SetMediaType(mediaType);
                DsError.ThrowExceptionForHR(hr);

                // Connect filters
                var pinOut = DsFindPin.ByDirection(_videoDevice, PinDirection.Output, 0);
                var pinIn = DsFindPin.ByDirection(sampleGrabberFilter, PinDirection.Input, 0);
                hr = _graphBuilder.Connect(pinOut, pinIn);
                DsError.ThrowExceptionForHR(hr);

                // Start capturing
                hr = _mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing camera", ex);
            }
        }

        private async Task<IBaseFilter> GetFirstVideoCaptureDeviceAsync()
        {
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            if (devices.Length == 0)
            {
                return null;
            }

            var device = devices[0];
            object source;
            var iid = typeof(IBaseFilter).GUID;
            device.Mon.BindToObject(null, null, ref iid, out source);
            return (IBaseFilter)source;
        }

        /// <summary>
        /// Creates necessary directories for image storage
        /// </summary>
        private void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(Path.Combine(_imageBasePath, _entrySubfolder));
                Directory.CreateDirectory(Path.Combine(_imageBasePath, _exitSubfolder));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create directories: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Captures image from camera and saves it with the specified ticket ID
        /// </summary>
        public async Task<string> CaptureEntryImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, true);
        }

        /// <summary>
        /// Captures image for vehicle exit and saves it with the specified ticket ID
        /// </summary>
        public async Task<string> CaptureExitImageAsync(string ticketId)
        {
            return await CaptureImageAsync(ticketId, false);
        }

        /// <summary>
        /// Internal method to capture and save image
        /// </summary>
        private async Task<string> CaptureImageAsync(string ticketId, bool isEntry)
        {
            try
            {
                if (_cameraType.Equals("Webcam", StringComparison.OrdinalIgnoreCase))
                {
                    // Get buffer size
                    int bufferSize = 0;
                    var hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
                    DsError.ThrowExceptionForHR(hr);

                    // Allocate buffer
                    var buffer = new byte[bufferSize];
                    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        // Get frame data
                        hr = _sampleGrabber.GetCurrentBuffer(ref bufferSize, handle.AddrOfPinnedObject());
                        DsError.ThrowExceptionForHR(hr);

                        // Get media type
                        var mediaType = new AMMediaType();
                        hr = _sampleGrabber.GetConnectedMediaType(mediaType);
                        DsError.ThrowExceptionForHR(hr);

                        try
                        {
                            var videoInfo = (VideoInfoHeader)Marshal.PtrToStructure(mediaType.formatPtr, typeof(VideoInfoHeader));
                            var width = videoInfo.BmiHeader.Width;
                            var height = videoInfo.BmiHeader.Height;

                            // Create filename
                            string filename = string.Format(_filenameFormat, ticketId, DateTime.Now) + ".jpg";
                            string targetSubfolder = isEntry ? _entrySubfolder : _exitSubfolder;
                            string imagePath = Path.Combine(_imageBasePath, targetSubfolder, filename);

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

                            // Save image
                            Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
                            bitmap.Save(imagePath, ImageFormat.Jpeg);

                            OnImageCaptured(new ImageCapturedEventArgs(ticketId, imagePath, isEntry));
                            return imagePath;
                        }
                        finally
                        {
                            DsUtils.FreeAMMediaType(mediaType);
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
                else if (_cameraType.Equals("IP", StringComparison.OrdinalIgnoreCase))
                {
                    // For IP camera, implement HTTP snapshot capture
                    // This is a placeholder for IP camera implementation
                    string url = $"http://{_cameraIp}:{_cameraPort}/snapshot.jpg";
                    
                    // Create filename
                    string filename = string.Format(_filenameFormat, ticketId, DateTime.Now) + ".jpg";
                    string targetSubfolder = isEntry ? _entrySubfolder : _exitSubfolder;
                    string imagePath = Path.Combine(_imageBasePath, targetSubfolder, filename);
                    
                    // Would typically use HttpClient to get image
                    // For now, just log the operation
                    Console.WriteLine($"Would capture IP camera image from {url} and save to {imagePath}");
                    
                    // Raise event with mock filepath
                    OnImageCaptured(new ImageCapturedEventArgs(ticketId, imagePath, isEntry));
                    
                    return imagePath;
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing image: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Opens the entry gate
        /// </summary>
        public bool OpenEntryGate()
        {
            return SendCommand("OPEN_ENTRY");
        }

        /// <summary>
        /// Opens the exit gate
        /// </summary>
        public bool OpenExitGate()
        {
            return SendCommand("A");
        }

        /// <summary>
        /// Sends command to the gate controller
        /// </summary>
        private bool SendCommand(string command)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                {
                    InitializeSerialPort();
                }

                _serialPort.WriteLine(command);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending command to gate controller: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Handles data received from serial port
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    string data = _serialPort.ReadLine().Trim();
                    Console.WriteLine($"Received from serial: {data}");
                    
                    // Parse command from serial data
                    if (data.StartsWith("IN:"))
                    {
                        string id = data.Substring(3);
                        OnCommandReceived(new CommandReceivedEventArgs("IN", id));
                    }
                    else if (data.StartsWith("OUT:"))
                    {
                        string id = data.Substring(4);
                        OnCommandReceived(new CommandReceivedEventArgs("OUT", id));
                    }
                    else if (data.StartsWith("STATUS:"))
                    {
                        string status = data.Substring(7);
                        OnCommandReceived(new CommandReceivedEventArgs("STATUS", status));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing serial data: {ex.Message}");
            }
        }

        /// <summary>
        /// Raises the ImageCaptured event
        /// </summary>
        protected virtual void OnImageCaptured(ImageCapturedEventArgs e)
        {
            ImageCaptured?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the CommandReceived event
        /// </summary>
        protected virtual void OnCommandReceived(CommandReceivedEventArgs e)
        {
            CommandReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Helper method to parse INI files
        /// </summary>
        private Dictionary<string, Dictionary<string, string>> ParseIniFile(string path)
        {
            var iniData = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";

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
        /// Helper method to get value from INI data
        /// </summary>
        private string GetIniValue(Dictionary<string, Dictionary<string, string>> iniData, string section, string key, string defaultValue)
        {
            if (iniData.ContainsKey(section) && iniData[section].ContainsKey(key))
            {
                return iniData[section][key];
            }
            return defaultValue;
        }

        /// <summary>
        /// Disposes hardware resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose pattern implementation
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_mediaControl != null)
                    {
                        _mediaControl.Stop();
                        Marshal.ReleaseComObject(_mediaControl);
                        _mediaControl = null;
                    }

                    if (_sampleGrabber != null)
                    {
                        Marshal.ReleaseComObject(_sampleGrabber);
                        _sampleGrabber = null;
                    }

                    if (_videoDevice != null)
                    {
                        Marshal.ReleaseComObject(_videoDevice);
                        _videoDevice = null;
                    }

                    if (_graphBuilder != null)
                    {
                        Marshal.ReleaseComObject(_graphBuilder);
                        _graphBuilder = null;
                    }

                    if (_serialPort != null)
                    {
                        if (_serialPort.IsOpen)
                            _serialPort.Close();
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                }

                _disposed = true;
            }
        }

        ~HardwareManager()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Event arguments for image capture events
    /// </summary>
    public class ImageCapturedEventArgs : EventArgs
    {
        public string TicketId { get; }
        public string ImagePath { get; }
        public bool IsEntryImage { get; }

        public ImageCapturedEventArgs(string ticketId, string imagePath, bool isEntryImage)
        {
            TicketId = ticketId;
            ImagePath = imagePath;
            IsEntryImage = isEntryImage;
        }
    }

    /// <summary>
    /// Event arguments for command received events
    /// </summary>
    public class CommandReceivedEventArgs : EventArgs
    {
        public string Command { get; }
        public string Data { get; }

        public CommandReceivedEventArgs(string command, string data)
        {
            Command = command;
            Data = data;
        }
    }
}
