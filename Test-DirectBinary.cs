using System;
using System.IO;
using System.Diagnostics;
using System.Text;

class TestDirectPrint
{
    static void Main()
    {
        Console.WriteLine("Test cetak langsung dari C# ke printer...");
        
        // Buat file binary dengan ESC/POS commands
        string tempFile = "direct-print-csharp.bin";
        using (var stream = new FileStream(tempFile, FileMode.Create))
        {
            // ESC @ - Initialize printer
            stream.WriteByte(0x1B);
            stream.WriteByte(0x40);
            
            // ESC a 1 - Center alignment
            stream.WriteByte(0x1B);
            stream.WriteByte(0x61);
            stream.WriteByte(0x01);
            
            // ESC ! 0 - Normal text
            stream.WriteByte(0x1B);
            stream.WriteByte(0x21);
            stream.WriteByte(0x00);
            
            // Text content
            string content = "\nTEST PRINTER DARI C#\n" +
                            "===================\n\n" +
                            "Ini adalah test cetak langsung\n" +
                            "melalui ESC/POS commands.\n" +
                            "Tanpa melalui PrintService.\n\n" +
                            $"{DateTime.Now}\n\n" +
                            "===================\n" +
                            "BERHASIL JIKA TERCETAK\n" +
                            "===================\n\n\n\n";
            
            byte[] contentBytes = Encoding.ASCII.GetBytes(content);
            stream.Write(contentBytes, 0, contentBytes.Length);
            
            // GS V B 1 - Partial cut with feed
            stream.WriteByte(0x1D);
            stream.WriteByte(0x56);
            stream.WriteByte(0x42);
            stream.WriteByte(0x01);
        }
        
        // Kirim ke printer
        string printerName = "TM-T82X-S-A";
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
        process.WaitForExit();
        
        Console.WriteLine($"Exit code: {process.ExitCode}");
        if (!string.IsNullOrEmpty(output))
            Console.WriteLine($"Output: {output}");
        if (!string.IsNullOrEmpty(error))
            Console.WriteLine($"Error: {error}");
        
        Console.WriteLine($"Perintah cetak dikirim ke {printerName}.");
        Console.WriteLine("Cek printer untuk hasilnya.");
    }
} 