using System;
using System.Diagnostics;

namespace TestEscPos
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Test Printer dengan ESC/POS Barcode");
                Console.WriteLine("====================================");
                
                // Buat ticket barcode
                string ticketNumber = "TEST-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                Console.WriteLine($"Nomor tiket: {ticketNumber}");
                
                // Mencetak barcode CODE128 dengan perintah ESC/POS
                Console.WriteLine("Mencetak barcode CODE-128 dengan ESC/POS commands...");
                RunScript("./print-escpos-barcode.sh", ticketNumber);
                
                // Sedikit delay
                Console.WriteLine("Menunggu 3 detik...");
                System.Threading.Thread.Sleep(3000);
                
                // Mencetak QR Code dengan perintah ESC/POS
                Console.WriteLine("Mencetak QR code dengan ESC/POS commands...");
                RunScript("./print-escpos-qrcode.sh", ticketNumber);
                
                Console.WriteLine("\nTest selesai!");
                Console.WriteLine("Barcode dan QR code seharusnya sudah dicetak di printer.");
                Console.WriteLine("Keduanya seharusnya bisa dipindai dengan scanner barcode/smartphone.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private static void RunScript(string scriptPath, string argument)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = argument,
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
                            Console.WriteLine("  Berhasil dicetak");
                            Console.WriteLine($"  {output.Trim().Split('\n')[^1]}"); // Tampilkan hanya baris terakhir output
                        }
                        else
                        {
                            Console.WriteLine($"  Error: {error}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal menjalankan script: {ex.Message}");
            }
        }
    }
} 