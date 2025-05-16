
namespace SANJET.Core.Interfaces
{

    public interface IRecordDialogService
    {
        (int deviceId, string deviceName, string username, int runcount) 
            ShowRecordDialog(int deviceId, string deviceName, string username,int runcount);
    }
}
