using System;
using System.Threading.Tasks;

partial class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("===== PARKING API TEST CLIENT =====");
        Console.WriteLine("This program will test the API connection and send test vehicle data");
        
        try
        {
            var client = new ParkingClient("http://192.168.2.6:5050");
            
            // Test connection
            Console.WriteLine("\n--- Testing Connection ---");
            bool connected = await client.TestConnection();
            
            if (!connected)
            {
                Console.WriteLine("Connection failed. Please check server availability and network connection.");
                return;
            }
            
            // Send test vehicle data
            Console.WriteLine("\n--- Sending Test Vehicle Data ---");
            string plateNumber = $"TEST-{DateTime.Now:HHmmss}";
            
            bool dataSent = await client.SendVehicleData(plateNumber, "Car");
            
            if (dataSent)
            {
                Console.WriteLine("Vehicle data sent successfully.");
                
                // Wait a bit for the data to be processed
                Console.WriteLine("Waiting 3 seconds for data processing...");
                await Task.Delay(3000);
                
                // Verify data was saved
                Console.WriteLine("\n--- Verifying Data Saved ---");
                bool verified = await client.VerifyDataSaved(plateNumber);
                
                if (verified)
                {
                    Console.WriteLine("SUCCESS: Data was properly saved in the database.");
                }
                else
                {
                    Console.WriteLine("WARNING: Data was sent but could not be verified in database.");
                    Console.WriteLine("This could indicate a problem with the server's data processing.");
                }
            }
            else
            {
                Console.WriteLine("Failed to send vehicle data.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: An unexpected error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        
        Console.WriteLine("\n===== TEST COMPLETED =====");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
} 