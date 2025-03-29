using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;
using System.Diagnostics;

namespace TestPrint
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Test Printer dengan Barcode");
                Console.WriteLine("===========================");
                
                // Buat ticket barcode
                string ticketNumber = "TEST-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                Console.WriteLine($"Nomor tiket: {ticketNumber}");
                
                // Buat direktori untuk barcode image
                string barcodeDir = Path.Combine(Directory.GetCurrentDirectory(), "images", "barcodes");
                Directory.CreateDirectory(barcodeDir);
                string barcodeFile = Path.Combine(barcodeDir, $"{ticketNumber}.png");
                
                // Generate barcode image
                GenerateBarcode(ticketNumber, barcodeFile);
                
                // Cetak tiket dengan text saja
                Console.WriteLine("Mencetak tiket text...");
                PrintTextTicket(ticketNumber);
                
                // Cetak barcode menggunakan script bitmap yang lebih baik
                Console.WriteLine("Mencetak barcode menggunakan script bitmap...");
                PrintBitmapBarcode(barcodeFile);
                
                Console.WriteLine("Test selesai!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private static void PrintTextTicket(string ticketNumber)
        {
            try
            {
                // Buat file yang akan dicetak
                string printContent = 
                    $"\n\n===== TEST BARCODE =====\n\n" +
                    $"Tiket    : {ticketNumber}\n" +
                    $"Tanggal  : {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                    $"Barcode  : <lihat gambar barcode>\n\n" +
                    $"========================\n\n\n\n\n";
                    
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, printContent);
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lp",
                    Arguments = $"-d TM-T82X-S-A -o raw {tempFile}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("Tiket berhasil dicetak.");
                            Console.WriteLine($"Output: {output}");
                        }
                        else
                        {
                            Console.WriteLine($"Error: {error}");
                        }
                    }
                }
                
                // Bersihkan file temporary
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // ignore deletion errors
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal mencetak tiket: {ex.Message}");
            }
        }
        
        private static void PrintBitmapBarcode(string barcodeFile)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "./print-bitmap-barcode.sh",
                    Arguments = barcodeFile,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("Barcode berhasil dicetak.");
                            Console.WriteLine($"Output: {output}");
                        }
                        else
                        {
                            Console.WriteLine($"Error: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal mencetak barcode: {ex.Message}");
            }
        }
        
        private static void GenerateBarcode(string content, string outputPath)
        {
            try
            {
                // Buat writer untuk barcode
                var barcodeWriter = new BarcodeWriterPixelData
                {
                    Format = BarcodeFormat.CODE_128,
                    Options = new EncodingOptions
                    {
                        Width = 300,
                        Height = 100,
                        Margin = 5,
                        PureBarcode = false
                    }
                };
                
                // Generate barcode
                var pixelData = barcodeWriter.Write(content);
                
                // Convert to image
                using (var image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(
                    pixelData.Pixels,
                    pixelData.Width,
                    pixelData.Height))
                {
                    // Save as PNG
                    using var stream = new FileStream(outputPath, FileMode.Create);
                    image.Save(stream, new PngEncoder());
                }
                
                Console.WriteLine($"Barcode berhasil dibuat: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal membuat barcode: {ex.Message}");
                throw;
            }
        }
    }
} 