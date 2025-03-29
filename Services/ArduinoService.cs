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