using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Threading;

class PrinterTest
{
    static void Main()
    {
        Console.WriteLine("==== PRINTER TEST SCRIPT ====");
        Console.WriteLine("This script will test printer functionality directly");
        Console.WriteLine();
        
        // Printer configuration
        string printerName = "TM-T82X-S-A"; // Verify this matches your configured printer
        Console.WriteLine($"Using printer: {printerName}");
        
        // Create receipt content
        string content = "TEST PRINTER FROM C#\n" +
                         "===================\n\n" +
                         "Testing direct printing\n" +
                         "from a standalone app.\n\n" +
                         $"Date/Time: {DateTime.Now}\n\n" +
                         "===================\n" +
                         "BERHASIL JIKA TERCETAK\n" +
                         "===================\n\n";
        Console.WriteLine("Content created");
        
        // Create binary file for ESC/POS commands
        Console.WriteLine("Creating binary file...");
        string tempFile = "test-print.bin";
        try
        {
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
            Console.WriteLine($"Binary file created: {tempFile}");
            Console.WriteLine($"File size: {new FileInfo(tempFile).Length} bytes");
            
            // Verify printer availability
            Console.WriteLine("Checking printer status...");
            var checkProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/lpstat",
                    Arguments = $"-p {printerName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            checkProcess.Start();
            string checkOutput = checkProcess.StandardOutput.ReadToEnd();
            checkProcess.WaitForExit();
            
            if (checkProcess.ExitCode == 0 && checkOutput.Contains(printerName))
            {
                Console.WriteLine($"Printer {printerName} is available");
                
                // Send to printer
                Console.WriteLine("Sending to printer...");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/lp",
                        Arguments = $"-d {printerName} -o raw {tempFile}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);
                
                Console.WriteLine($"Exit code: {process.ExitCode}");
                
                if (!string.IsNullOrEmpty(output))
                    Console.WriteLine($"Output: {output}");
                    
                if (!string.IsNullOrEmpty(error))
                    Console.WriteLine($"Error: {error}");
                
                if (process.ExitCode == 0)
                    Console.WriteLine("Print job sent successfully!");
                else
                    Console.WriteLine("Failed to send print job.");
            }
            else
            {
                Console.WriteLine($"Printer {printerName} is not available");
                Console.WriteLine($"lpstat output: {checkOutput}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine();
        Console.WriteLine("==== TEST COMPLETED ====");
    }
} 