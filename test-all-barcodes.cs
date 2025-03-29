using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace TestAllBarcodes
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("=== PRINTER BARCODE TEST TOOL ===");
                Console.WriteLine("=================================");
                
                // Generate test code
                string testCode = "TEST-" + DateTime.Now.ToString("yyyyMMddHHmm");
                
                // Menu
                Console.WriteLine("\nTest code: " + testCode);
                Console.WriteLine("\nMemilih jenis test yang akan dijalankan:");
                Console.WriteLine("1. Regular Barcode (CODE-128)");
                Console.WriteLine("2. Simple QR Code");
                Console.WriteLine("3. Big QR Code");
                Console.WriteLine("4. Multiple QR Code Sizes");
                Console.WriteLine("5. Run ALL Tests");
                Console.WriteLine("0. Exit");
                
                Console.Write("\nPilihan Anda: ");
                string input = Console.ReadLine();
                
                switch (input)
                {
                    case "1":
                        Console.WriteLine("\nMenjalankan test CODE-128 barcode...");
                        RunScript("./print-barcode-code128.sh", testCode);
                        break;
                        
                    case "2":
                        Console.WriteLine("\nMenjalankan test Simple QR Code...");
                        RunScript("./print-simple-qrcode.sh", testCode);
                        break;
                        
                    case "3":
                        Console.WriteLine("\nMenjalankan test Big QR Code...");
                        RunScript("./print-qrcode-big.sh", testCode);
                        break;
                        
                    case "4":
                        Console.WriteLine("\nMenjalankan test Multiple QR Codes...");
                        RunScript("./print-qrcode-multiple.sh", testCode, "4,6,8,10");
                        break;
                        
                    case "5":
                        Console.WriteLine("\nMenjalankan SEMUA test...");
                        Console.WriteLine("\n1. CODE-128 Barcode");
                        RunScript("./print-barcode-code128.sh", testCode);
                        Thread.Sleep(3000);
                        
                        Console.WriteLine("\n2. Simple QR Code");
                        RunScript("./print-simple-qrcode.sh", testCode);
                        Thread.Sleep(3000);
                        
                        Console.WriteLine("\n3. Big QR Code");
                        RunScript("./print-qrcode-big.sh", testCode);
                        Thread.Sleep(3000);
                        
                        Console.WriteLine("\n4. Multiple QR Codes");
                        RunScript("./print-qrcode-multiple.sh", testCode, "4,6,8,10");
                        break;
                        
                    case "0":
                        Console.WriteLine("\nKeluar dari aplikasi...");
                        return;
                        
                    default:
                        Console.WriteLine("\nPilihan tidak valid!");
                        break;
                }
                
                Console.WriteLine("\nTest selesai!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        
        private static void RunScript(string scriptPath, string argument, string argument2 = null)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    Arguments = argument2 == null ? argument : $"{argument} {argument2}",
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
                            
                            // Tampilkan beberapa baris output, maksimal 5 baris
                            string[] lines = output.Split('\n');
                            int startLine = Math.Max(0, lines.Length - 5);
                            for (int i = startLine; i < lines.Length; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(lines[i]))
                                {
                                    Console.WriteLine($"  {lines[i]}");
                                }
                            }
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