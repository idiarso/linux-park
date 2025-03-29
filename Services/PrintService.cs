using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Diagnostics;
using ParkIRC.Models;
using System.IO;
using System.Threading;

namespace ParkIRC.Services
{
    public class PrintService
    {
        private readonly ILogger<PrintService> _logger;
        private readonly IConfiguration _config;
        private readonly string _printerName;
        private readonly string _driverName;
        private readonly int _maxRetries;

        public PrintService(ILogger<PrintService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _printerName = config["PrinterSettings:PrinterName"] ?? config["Printer:DefaultName"] ?? "TM-T82X-S-A";
            _driverName = config["Printer:Driver"] ?? "escpos";
            _maxRetries = config.GetValue<int>("PrinterSettings:MaxRetries", 3);
            
            _logger.LogInformation($"PrintService initialized with printer: {_printerName}");
        }

        public bool PrintTicket(string content)
        {
            // Coba beberapa pendekatan secara berurutan jika yang sebelumnya gagal
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                _logger.LogInformation($"Print attempt {attempt} of {_maxRetries}");
                
                try
                {
                    // Pendekatan 1: Menggunakan metode binary direct
                    if (attempt == 1)
                    {
                        if (PrintBinaryDirect(content))
                        {
                            _logger.LogInformation("Print successful using binary direct method");
                            return true;
                        }
                    }
                    // Pendekatan 2: Menggunakan metode file temporary
                    else if (attempt == 2)
                    {
                        if (PrintViaFile(content))
                        {
                            _logger.LogInformation("Print successful using temporary file method");
                            return true;
                        }
                    }
                    // Pendekatan 3: Menggunakan metode shell script
                    else
                    {
                        if (PrintViaScript(content))
                        {
                            _logger.LogInformation("Print successful using shell script method");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in print attempt {attempt}");
                }
                
                // Tunggu sebentar sebelum mencoba lagi
                if (attempt < _maxRetries)
                {
                    Thread.Sleep(1000);
                }
            }
            
            _logger.LogError("All print attempts failed");
            return false;
        }

        private bool PrintBinaryDirect(string content)
        {
            _logger.LogDebug("Using binary direct printing method");
            
            try
            {
                string tempFile = Path.GetTempFileName();
                
                try 
                {
                    // Gunakan metode binary langsung untuk ESC/POS commands
                    using (var stream = new FileStream(tempFile, FileMode.Create))
                    {
                        // Initialize printer (ESC @)
                        stream.WriteByte(0x1B);
                        stream.WriteByte(0x40);
                        
                        // Set centering (ESC a 1)
                        stream.WriteByte(0x1B);
                        stream.WriteByte(0x61);
                        stream.WriteByte(0x01);
                        
                        // Text content
                        byte[] contentBytes = Encoding.UTF8.GetBytes(content);
                        stream.Write(contentBytes, 0, contentBytes.Length);
                        
                        // Line feeds at the end
                        byte[] linefeeds = Encoding.ASCII.GetBytes("\n\n\n\n");
                        stream.Write(linefeeds, 0, linefeeds.Length);
                        
                        // Cut paper (GS V 66 1)
                        stream.WriteByte(0x1D);
                        stream.WriteByte(0x56);
                        stream.WriteByte(0x42);
                        stream.WriteByte(0x01);
                    }
                    
                    // Kirim ke printer menggunakan lp command
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "/usr/bin/lp",
                            Arguments = $"-d {_printerName} -o raw {tempFile}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit(10000); // Wait up to 10 seconds
                    
                    if (!string.IsNullOrEmpty(output))
                        _logger.LogDebug($"Printer output: {output}");
                        
                    if (!string.IsNullOrEmpty(error))
                        _logger.LogError($"Printer error: {error}");

                    // Buat salinan file cetak untuk debugging
                    try {
                        string debugCopyFile = $"last_print_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
                        File.Copy(tempFile, debugCopyFile);
                        _logger.LogDebug($"Copied print file to {debugCopyFile} for debugging");
                    }
                    catch (Exception ex) {
                        _logger.LogWarning($"Could not create debug copy of print file: {ex.Message}");
                    }

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"Printing failed with exit code: {process.ExitCode}, Error: {error}");
                        return false;
                    }
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in binary direct printing method");
                    return false;
                }
                finally
                {
                    // Hapus file temporary
                    try { File.Delete(tempFile); } 
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating temporary file for binary direct printing");
                return false;
            }
        }

        private bool PrintViaFile(string content)
        {
            _logger.LogDebug("Using temporary file printing method");
            
            try
            {
                // Buat file teks biasa tanpa ESC/POS commands
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, content);
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/lp",
                        Arguments = $"-d {_printerName} {tempFile}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                process.WaitForExit(10000);
                
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                
                if (!string.IsNullOrEmpty(error))
                    _logger.LogError($"PrintViaFile error: {error}");
                
                // Hapus file temporary
                try { File.Delete(tempFile); } 
                catch { /* ignore */ }
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in temporary file printing method");
                return false;
            }
        }

        private bool PrintViaScript(string content)
        {
            _logger.LogDebug("Using shell script printing method");
            
            try
            {
                // Buat shell script untuk printing
                string scriptFile = Path.GetTempFileName() + ".sh";
                string contentFile = Path.GetTempFileName() + ".txt";
                
                // Simpan konten ke file
                File.WriteAllText(contentFile, content);
                
                // Buat shell script
                StringBuilder script = new StringBuilder();
                script.AppendLine("#!/bin/bash");
                script.AppendLine($"# Auto-generated print script for {_printerName}");
                script.AppendLine("echo \"=== Print Script Started ===\"");
                script.AppendLine($"echo -ne \"\\x1B\\x40\" > /tmp/print-data.bin");  // Initialize
                script.AppendLine($"echo -ne \"\\x1B\\x61\\x01\" >> /tmp/print-data.bin");  // Center
                script.AppendLine($"cat \"{contentFile}\" >> /tmp/print-data.bin");  // Content
                script.AppendLine($"echo -ne \"\\n\\n\\n\\n\" >> /tmp/print-data.bin");  // Feed
                script.AppendLine($"echo -ne \"\\x1D\\x56\\x42\\x01\" >> /tmp/print-data.bin");  // Cut
                script.AppendLine($"lp -d {_printerName} -o raw /tmp/print-data.bin");
                script.AppendLine("echo \"=== Print Script Completed ===\"");
                
                File.WriteAllText(scriptFile, script.ToString());
                
                // Make executable
                var chmodProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/chmod",
                        Arguments = $"+x {scriptFile}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                chmodProcess.Start();
                chmodProcess.WaitForExit();
                
                // Run script
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = scriptFile,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(15000);
                
                _logger.LogDebug($"Script output: {output}");
                if (!string.IsNullOrEmpty(error))
                    _logger.LogError($"Script error: {error}");
                
                // Hapus file temporary
                try 
                { 
                    File.Delete(scriptFile);
                    File.Delete(contentFile);
                    File.Delete("/tmp/print-data.bin");
                } 
                catch { /* ignore */ }
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in shell script printing method");
                return false;
            }
        }

        public string GetCurrentPrinter()
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "lpstat",
                    Arguments = "-d",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    if (output.Contains("system default destination:"))
                    {
                        return output.Split(':')[1].Trim();
                    }
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> PrintExitReceipt(ExitReceiptData data)
        {
            try
            {
                var content = new StringBuilder();
                content.AppendLine("STRUK PARKIR - KELUAR");
                content.AppendLine("===================");
                content.AppendLine();
                content.AppendLine($"No. Transaksi: {data.TransactionId}");
                content.AppendLine($"No. Kendaraan: {data.VehicleNumber}");
                content.AppendLine();
                content.AppendLine($"Waktu Masuk : {data.EntryTime:dd/MM/yyyy HH:mm}");
                content.AppendLine($"Waktu Keluar: {data.ExitTime:dd/MM/yyyy HH:mm}");
                content.AppendLine($"Durasi      : {data.Duration}");
                content.AppendLine();
                content.AppendLine($"Total Biaya : Rp {data.Amount:N0}");
                content.AppendLine();
                content.AppendLine("===================");
                content.AppendLine("Terima Kasih");
                content.AppendLine();
                content.AppendLine();
                content.AppendLine();

                return PrintTicket(content.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing exit receipt");
                return false;
            }
        }
    }
} 