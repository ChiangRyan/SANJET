
namespace SANJET.Core.Models
{
    public class DeviceRecord
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Username { get; set; }
        public int RunCount { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        
    }
}