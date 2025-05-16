using CommunityToolkit.Mvvm.ComponentModel;
using System.Net;
using System.Windows.Input;
using System.Xml.Linq;

namespace SANJET.Core.Models
{
    public class DeviceDataChangedEventArgs : EventArgs
    {
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public int SlaveId { get; set; }
        public int RunCount { get; set; }
        public bool IsOperational { get; set; }
    }

    public partial class DeviceModel : ObservableObject
    {
        public int Id { get; private set; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _ipAddress;

        [ObservableProperty]
        private int _slaveId;

        [ObservableProperty]
        private int _runCount;

        [ObservableProperty]
        private string _status;

        [ObservableProperty]
        private bool _isOperational;

        [ObservableProperty]
        private ICommand? _startCommand;

        [ObservableProperty]
        private ICommand? _stopCommand;

        [ObservableProperty]
        private ICommand? _recordCommand;

        public event EventHandler<DeviceDataChangedEventArgs>? DataChanged;

        public DeviceModel(int id, string initialName, string initialIpAddress, int initialSlaveId, bool initialIsOperational = false, int initialRunCount = 0, string initialStatus = "未知")
        {
            Id = id;
            _name = initialName;
            _ipAddress = initialIpAddress;
            _slaveId = initialSlaveId;
            _runCount = initialRunCount;
            _status = initialStatus;
            _isOperational = initialIsOperational;
        }

        partial void OnNameChanged(string value) => NotifyDataChanged();
        partial void OnIpAddressChanged(string value) => NotifyDataChanged();
        partial void OnSlaveIdChanged(int value) => NotifyDataChanged();
        partial void OnRunCountChanged(int value) => NotifyDataChanged();
        partial void OnIsOperationalChanged(bool value) => NotifyDataChanged();

        private void NotifyDataChanged()
        {
            DataChanged?.Invoke(this, new DeviceDataChangedEventArgs
            {
                Name = Name,
                IpAddress = IpAddress,
                SlaveId = SlaveId,
                RunCount = RunCount,
                IsOperational = IsOperational
            });
        }
    }
}