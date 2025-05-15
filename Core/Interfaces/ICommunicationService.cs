using SANJET.SANJET.Core.Models;

namespace SANJET.Core.Interfaces
{
    public interface ICommunicationService
    {

        Task<ModbusReadResult> ReadModbusAsync(string ip, int slaveId, int address, int quantity, int functionCode);
        Task<string> WriteModbusAsync(string ip, int slaveId, int address, int value, int functionCode);
        Task<string> SendLedCommandAsync(string ip, string state);
        void CleanupConnections();
    }
}
