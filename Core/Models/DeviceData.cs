
namespace SANJET.SANJET.Core.Models
{
    public class DeviceData
    {
        public int Id { get; set; }
        public string Name { get; set; } // 改為 Name，與程式碼一致
        public string IpAddress { get; set; }
        public int SlaveId { get; set; }
        public bool IsOperational { get; set; }
        public int RunCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}