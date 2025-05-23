﻿using System.Text.Json.Serialization; // 引入此命名空間以使用 JsonPropertyName

namespace SANJET.Core.Models
{
    public class ModbusReadResult
    {
        [JsonPropertyName("Status")] 
        public string Status { get; set; } = string.Empty; // 狀態碼

        [JsonPropertyName("SlaveId")]
        public int SlaveId { get; set; }

        [JsonPropertyName("Data")]
        public List<int> Data { get; set; } = []; // 讀取的寄存器值

        public DateTime Timestamp { get; set; }// 時間戳

        public string Message { get; set; } = string.Empty; // 新增：錯誤訊息
    }
}