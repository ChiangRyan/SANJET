using System;
using System.Windows;
using System.Diagnostics;
using SANJET.SANJET.Core.Interfaces;
using SANJET.UI.Views.Windows;


namespace SANJET.SANJET.Core.Services
{
    public class RecordService : IRecordDialogService
    {
        private readonly SqliteDataService _dataService;

        public RecordService(SqliteDataService dataService)
        {
            _dataService = dataService;
        }

        public (int deviceId, string deviceName, string username, int runcount) 

            ShowRecordDialog(int deviceId, string deviceName, string username, int runcount)
        {
            try
            {
                Debug.WriteLine($"ShowRecordDialog: DeviceId={deviceId}, DeviceName={deviceName}, Username={username}, RunCount={runcount}");
                if (!_dataService.DeviceExists(deviceId))
                {
                    Debug.WriteLine($"無效的 DeviceId: {deviceId}");
                    MessageBox.Show($"設備 ID {deviceId} 不存在於資料庫中", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (deviceId, deviceName, username, runcount);
                }

                var recordView = new RecordView(deviceId, deviceName, username, runcount, _dataService);
                recordView.ShowDialog();
                return (deviceId, deviceName, username, runcount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ShowRecordDialog failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"顯示記錄視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return (deviceId, deviceName, username, runcount);
            }
        }
    }
}
