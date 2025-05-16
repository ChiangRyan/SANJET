
namespace SANJET.Core.Models
{
    public class DeviceRecord
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int RunCount { get; set; } 
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
    }
}