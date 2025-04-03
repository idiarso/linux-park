using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ParkingClient
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly ILogger _logger;

    public ParkingClient(string baseUrl = "http://192.168.2.6:5050", ILogger logger = null)
    {
        _baseUrl = baseUrl;
        _logger = logger;
        _client = new HttpClient();
        
        // Configure client
        _client.Timeout = TimeSpan.FromSeconds(30);
        
        // Add authentication
        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes("admin:admin"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        
        // Disable caching
        _client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true,
            MaxAge = TimeSpan.Zero
        };
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            LogInfo("Testing connection to server...");
            var response = await _client.GetAsync($"{_baseUrl}/api/test-connection");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                LogInfo($"Connection successful: {content}");
                return true;
            }
            else
            {
                LogError($"Connection failed: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogError($"Connection error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendVehicleData(string plateNumber, string vehicleType = "Motorcycle")
    {
        try
        {
            // Generate a unique ID for testing
            var vehicleId = $"{plateNumber.Replace(" ", "")}-{DateTime.Now:yyyyMMddHHmmss}";
            
            // Create payload
            var payload = new
            {
                VehicleId = vehicleId,
                PlateNumber = plateNumber,
                VehicleType = vehicleType,
                Timestamp = DateTime.Now
            };
            
            LogInfo($"Sending vehicle data: {JsonConvert.SerializeObject(payload)}");
            
            // Create request content
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(payload),
                Encoding.UTF8,
                "application/json");
            
            // Send request with retry
            int maxRetries = 3;
            int retryCount = 0;
            bool success = false;
            
            while (!success && retryCount < maxRetries)
            {
                try
                {
                    retryCount++;
                    LogInfo($"Attempt {retryCount} of {maxRetries}...");
                    
                    var response = await _client.PostAsync($"{_baseUrl}/api/parking", jsonContent);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        LogInfo($"Request successful: {responseContent}");
                        success = true;
                    }
                    else
                    {
                        LogError($"Request failed ({response.StatusCode}): {responseContent}");
                        
                        if (retryCount < maxRetries)
                        {
                            int delayMs = retryCount * 1000;
                            LogInfo($"Retrying in {delayMs}ms...");
                            await Task.Delay(delayMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Request exception: {ex.Message}");
                    
                    if (retryCount < maxRetries)
                    {
                        int delayMs = retryCount * 1000;
                        LogInfo($"Retrying in {delayMs}ms...");
                        await Task.Delay(delayMs);
                    }
                }
            }
            
            return success;
        }
        catch (Exception ex)
        {
            LogError($"Error sending vehicle data: {ex.Message}");
            return false;
        }
    }
    
    public async Task<bool> VerifyDataSaved(string plateNumber)
    {
        try
        {
            LogInfo($"Verifying data saved for plate: {plateNumber}");
            var response = await _client.GetAsync($"{_baseUrl}/api/vehicles");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                LogInfo($"Retrieved vehicles: {content}");
                
                // Parse the response and check if our plate number exists
                var result = JsonConvert.DeserializeAnonymousType(content, new 
                {
                    success = false,
                    count = 0,
                    data = new[] { new { VehicleNumber = "" } }
                });
                
                if (result?.success == true && result.count > 0)
                {
                    bool found = result.data.Any(v => v.VehicleNumber == plateNumber);
                    LogInfo($"Vehicle with plate {plateNumber} found: {found}");
                    return found;
                }
                else
                {
                    LogError("Failed to verify data: Invalid response format");
                    return false;
                }
            }
            else
            {
                LogError($"Failed to verify data: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogError($"Error verifying data: {ex.Message}");
            return false;
        }
    }
    
    private void LogInfo(string message)
    {
        Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
        _logger?.LogInformation(message);
    }
    
    private void LogError(string message)
    {
        Console.WriteLine($"[ERROR] {DateTime.Now}: {message}");
        _logger?.LogError(message);
    }
} 