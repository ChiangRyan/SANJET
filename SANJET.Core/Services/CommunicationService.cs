using SANJET.SANJET.Core.Interfaces;
using SANJET.SANJET.Core.Models;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;


namespace SANJET.SANJET.Core.Services
{
    public class CommunicationService : ICommunicationService
    {
        private static readonly HttpClient client;

        static CommunicationService()
        {
            client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5) // 設置 5 秒超時
            };
        }

        public async Task<ModbusReadResult> ReadModbusAsync(string ip, int slaveId, int address, int quantity, int functionCode)
        {
            try
            {
                string url = $"http://{ip}/readModbus?slaveId={slaveId}&address={address}&quantity={quantity}&functionCode={functionCode}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    var modbusResult = JsonSerializer.Deserialize<ModbusReadResult>(responseBody);
                    modbusResult.Timestamp = DateTime.Now;
                    return modbusResult;
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON deserialization failed: {ex.Message}");
                    return new ModbusReadResult { Status = "error", Message = "Invalid response format" };
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HTTP request failed: {ex.Message}");
                return new ModbusReadResult { Status = "error", Message = ex.Message };
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Request timed out: {ex.Message}");
                return new ModbusReadResult { Status = "error", Message = "Request timed out" };
            }
        }

        public async Task<string> WriteModbusAsync(string ip, int slaveId, int address, int value, int functionCode)
        {
            try
            {
                string url = $"http://{ip}/writeModbus?slaveId={slaveId}&address={address}&value={value}&functionCode={functionCode}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HTTP request failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Request timed out: {ex.Message}");
                return "Error: Request timed out";
            }
        }

        public async Task<string> SendLedCommandAsync(string ip, string state)
        {
            try
            {
                string url = $"http://{ip}/setLED?state={state}";
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"HTTP request failed: {ex.Message}");
                return $"Error: {ex.Message}";
            }
            catch (TaskCanceledException ex)
            {
                Debug.WriteLine($"Request timed out: {ex.Message}");
                return "Error: Request timed out";
            }
        }

        // 實現連線清理方法
        public void CleanupConnections()
        {
            // 取消所有未完成的請求
            client.CancelPendingRequests();
            Debug.WriteLine("Pending HTTP requests canceled.");
        }

        // 移除 Dispose 方法，因為靜態 HttpClient 不應被 Dispose
        // public void Dispose()
        // {
        //     client.Dispose();
        // }
    }
}
