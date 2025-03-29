using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParkIRC.Services
{
    public class SerialPortOptions
    {
        public string PortName { get; set; } = "/dev/ttyUSB0";
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public int ReadTimeout { get; set; } = 2000;
        public int WriteTimeout { get; set; } = 2000;
        public bool AutoReconnect { get; set; } = true;
        public int ReconnectInterval { get; set; } = 5000;
    }

    public class SerialPortService : IDisposable
    {
        private readonly ILogger<SerialPortService> _logger;
        private readonly SerialPortOptions _options;
        private SerialPort _serialPort;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
        private Timer _reconnectTimer;
        private bool _disposed = false;

        public SerialPortService(
            IOptions<SerialPortOptions> options,
            ILogger<SerialPortService> logger)
        {
            _options = options.Value;
            _logger = logger;

            InitializeSerialPort();

            if (_options.AutoReconnect)
            {
                _reconnectTimer = new Timer(ReconnectCallback, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void InitializeSerialPort()
        {
            try
            {
                _serialPort = new SerialPort
                {
                    PortName = _options.PortName,
                    BaudRate = _options.BaudRate,
                    DataBits = _options.DataBits,
                    Parity = _options.Parity,
                    StopBits = _options.StopBits,
                    ReadTimeout = _options.ReadTimeout,
                    WriteTimeout = _options.WriteTimeout,
                    NewLine = "\n"
                };

                _serialPort.DataReceived += SerialPort_DataReceived;

                OpenSerialPort();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing serial port");
                ScheduleReconnect();
            }
        }

        private void OpenSerialPort()
        {
            try
            {
                if (_serialPort != null && !_serialPort.IsOpen)
                {
                    _serialPort.Open();
                    _logger.LogInformation($"Serial port {_options.PortName} opened");
                    
                    if (_reconnectTimer != null)
                    {
                        _reconnectTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening serial port {_options.PortName}");
                ScheduleReconnect();
            }
        }

        private void ScheduleReconnect()
        {
            if (_options.AutoReconnect && _reconnectTimer != null)
            {
                _reconnectTimer.Change(_options.ReconnectInterval, Timeout.Infinite);
            }
        }

        private void ReconnectCallback(object state)
        {
            _logger.LogInformation("Attempting to reconnect to Arduino...");
            OpenSerialPort();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    string data = _serialPort.ReadLine().Trim();
                    _logger.LogDebug($"Received from Arduino: {data}");

                    ProcessResponse(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading data from serial port");
            }
        }

        private void ProcessResponse(string response)
        {
            // Handle response based on its prefix
            if (response.StartsWith("OK:") || response.StartsWith("ERR:"))
            {
                // This is a response to a command, find the matching TaskCompletionSource
                string commandType = response.Substring(0, response.IndexOf(':'));
                
                foreach (var kvp in _pendingResponses)
                {
                    var tcs = kvp.Value;
                    if (!tcs.Task.IsCompleted)
                    {
                        tcs.TrySetResult(response);
                        _pendingResponses.TryRemove(kvp.Key, out _);
                        break;
                    }
                }
            }
            else if (response.StartsWith("EVENT:"))
            {
                // This is an event notification
                string eventType = response.Substring(6);
                OnArduinoEvent?.Invoke(this, new ArduinoEventArgs(eventType));
            }
            else if (response.StartsWith("STATUS:"))
            {
                // This is a status response
                foreach (var kvp in _pendingResponses.ToArray())
                {
                    if (kvp.Key == "STATUS")
                    {
                        var tcs = kvp.Value;
                        if (!tcs.Task.IsCompleted)
                        {
                            tcs.TrySetResult(response);
                            _pendingResponses.TryRemove(kvp.Key, out _);
                            break;
                        }
                    }
                }
            }
            else if (response == "READY:GATE_CONTROLLER")
            {
                _logger.LogInformation("Arduino gate controller is ready");
                OnArduinoEvent?.Invoke(this, new ArduinoEventArgs("READY"));
            }
        }

        public async Task<bool> SendCommandAsync(string command)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsConnected())
                {
                    await ReconnectAsync();
                    if (!IsConnected())
                    {
                        return false;
                    }
                }

                string commandId = Guid.NewGuid().ToString();
                var tcs = new TaskCompletionSource<string>();
                _pendingResponses[commandId] = tcs;

                _logger.LogDebug($"Sending command to Arduino: {command}");
                _serialPort.WriteLine(command);

                // Wait for response with timeout
                var timeoutTask = Task.Delay(_options.WriteTimeout);
                var responseTask = tcs.Task;

                var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _pendingResponses.TryRemove(commandId, out _);
                    _logger.LogWarning($"Command timed out: {command}");
                    return false;
                }

                string response = await responseTask;
                return response.StartsWith("OK:");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending command to Arduino: {command}");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<string> SendCommandAndWaitForResponseAsync(string command)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!IsConnected())
                {
                    await ReconnectAsync();
                    if (!IsConnected())
                    {
                        return "ERR:NOT_CONNECTED";
                    }
                }

                var tcs = new TaskCompletionSource<string>();
                _pendingResponses[command] = tcs;

                _logger.LogDebug($"Sending command to Arduino: {command}");
                _serialPort.WriteLine(command);

                // Wait for response with timeout
                var timeoutTask = Task.Delay(_options.WriteTimeout);
                var responseTask = tcs.Task;

                var completedTask = await Task.WhenAny(responseTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    _pendingResponses.TryRemove(command, out _);
                    _logger.LogWarning($"Command timed out: {command}");
                    return "ERR:TIMEOUT";
                }

                return await responseTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending command to Arduino: {command}");
                return $"ERR:{ex.Message}";
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private bool IsConnected()
        {
            return _serialPort != null && _serialPort.IsOpen;
        }

        private async Task ReconnectAsync()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }

                    _serialPort.Open();
                    _logger.LogInformation($"Reconnected to serial port {_options.PortName}");
                    
                    // Wait a moment for Arduino to initialize
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconnecting to serial port");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reconnectTimer?.Dispose();
                    
                    if (_serialPort != null)
                    {
                        _serialPort.DataReceived -= SerialPort_DataReceived;
                        
                        if (_serialPort.IsOpen)
                        {
                            _serialPort.Close();
                        }
                        
                        _serialPort.Dispose();
                    }
                    
                    _semaphore.Dispose();
                }

                _disposed = true;
            }
        }

        // Event for Arduino events
        public event EventHandler<ArduinoEventArgs> OnArduinoEvent;
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