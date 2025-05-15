using System.Windows;
using System.Diagnostics;
using SANJET.Core.ViewModels;
using SANJET.Core.Services;

namespace SANJET.UI.Views.Windows
{
    public partial class RecordView : Window
    {
        private RecordViewModel _viewModel;

        public RecordView(int deviceId, string deviceName, string currentUser,int runcount, SqliteDataService dataService)
        {
            Debug.WriteLine($"RecordView 初始化: DeviceId={deviceId}, DeviceName={deviceName}, Username={currentUser}");
            var deviceRecords = dataService.GetDeviceRecords(deviceId);
            _viewModel = new RecordViewModel(deviceRecords, deviceId, deviceName, currentUser, runcount, dataService);
            this.DataContext = _viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}