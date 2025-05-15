
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

using System.Windows;

using SANJET.Core.Enums;
using System.IO;


namespace SANJET.SANJET.Core.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class HomeViewModel : ViewModelBase
    {
        private readonly ICommunicationService _communicationService;
        private readonly SqliteDataService _dataService;
        private readonly PermissionService _permissionService;
        private readonly IRecordDialogService _recorddialogService;
        private readonly ITextToSpeechService _textToSpeechService; // Add TTS service
        private readonly IAudioPlayerService _audioPlayerService; // Add TTS service

        // 添加控制設備的權限屬性
        public bool CanControlDevice => _permissionService.HasPermission(Permission.ControlDevice);

        private readonly Timer _updateTimer;

        //Mapping Modbus位置設置
        private readonly int _runCountAddress = 10;
        private readonly int _statusAddress = 1;
        private readonly int _controlAddress = 0;

        public ObservableCollection<DeviceModel> Devices { get; set; }

        // 添加语音播报的预设文本
        public string StartAnnouncement { get; set; } = "啟動中，請注意安全"; // 默认启动播报文本
        public string StopAnnouncement { get; set; } = "停止中，請注意安全"; // 默认停止播报文本
        public bool EnableVoiceAnnouncement { get; set; } = true; // 语音播报开关

        public HomeViewModel(
            ICommunicationService communicationService,
            SqliteDataService dataService,
            IRecordDialogService RecordDialogService,
            PermissionService permissionService,
            ITextToSpeechService textToSpeechService,  // 添加文字转语音服务参数
            IAudioPlayerService audioPlayerService // 添加音频播放器服务参数
            )

        {
            _communicationService = communicationService;
            _dataService = dataService;
            _recorddialogService = RecordDialogService;
            _permissionService = permissionService;
            _textToSpeechService = textToSpeechService; // 设置文字转语音服务
            _audioPlayerService = audioPlayerService; // 设置音频播放器服务


            // 設置語音速度
            _textToSpeechService.Rate = -1;

            // 訂閱權限變更事件
            _permissionService.PermissionsChanged += (s, e) =>
            {
                Debug.WriteLine($"PermissionsChanged event triggered: CanControlDevice={CanControlDevice}");
                OnPropertyChanged(nameof(CanControlDevice));
                // 如果失去控制權限，停止輪詢
                if (!CanControlDevice)
                {
                    StopPolling();
                    Debug.WriteLine("Polling stopped due to lack of ControlDevice permission.");
                }
            };

            Devices = new ObservableCollection<DeviceModel>();

            // 先從資料庫載入設備數據
            LoadDevicesFromDatabase();

            // 如果資料庫中沒有設備數據，則初始化預設設備
            if (!Devices.Any())
            {
                InitializeDefaultDevices();
            }

            _updateTimer = new Timer(5000);
            _updateTimer.Elapsed += async (s, e) => await UpdateDeviceData();
            _updateTimer.AutoReset = true;
        }

        // 播放语音提示
        private void AnnounceDevice(DeviceModel device, string action)
        {
            if (!EnableVoiceAnnouncement) return;

            try
            {
                string announcement = $"設備 {device.Name} {action}";
                _textToSpeechService.Speak(announcement);
                Debug.WriteLine($"语音播报: {announcement}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"语音播报错误: {ex.Message}");
                // 不向用户显示错误，保持程序继续运行
            }
        }


        // 播放音频提示
        private void PlayAudioAnnouncement(string action)
        {
            if (!EnableVoiceAnnouncement) return;
            try
            {
                string audioFileName = "SquidGame1.mp3";
                string audioFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, audioFileName);
                _audioPlayerService.Play(audioFilePath);
                Debug.WriteLine($"音频播报: {audioFilePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"音频播报错误: {ex.Message}");
                // 不向用户显示错误，保持程序继续运行
            }
        }


        private void LoadDevicesFromDatabase()
        {
            var deviceDataList = _dataService.GetDeviceData();
            foreach (var data in deviceDataList)
            {
                string ipAddress;
                int slaveId;
                if (deviceDataList.IndexOf(data) < 10)
                {
                    ipAddress = "192.168.64.52";
                    slaveId = deviceDataList.IndexOf(data) + 1;
                }
                else if (deviceDataList.IndexOf(data) == 10)
                {
                    ipAddress = "192.168.64.87";
                    slaveId = 1;
                }
                else
                {
                    ipAddress = "192.168.64.89";
                    slaveId = 1;
                }

                var device = new DeviceModel
                {
                    Id = data.Id, // 使用資料庫中的 ID，確保 ID 從資料庫中正確載入
                    Name = data.Name ?? $"設備 {deviceDataList.IndexOf(data) + 1}", // 使用資料庫名稱，無則用預設
                    IpAddress = data.IpAddress ?? ipAddress,
                    SlaveId = data.SlaveId > 0 ? data.SlaveId : slaveId,
                    RunCount = data.RunCount,
                    Status = "未知",
                    IsOperational = data.IsOperational
                };

                int deviceIndex = deviceDataList.IndexOf(data);
                device.StartCommand = new RelayCommand(async () => await ExecuteStart(deviceIndex));
                device.StopCommand = new RelayCommand(async () => await ExecuteStop(deviceIndex));
                device.RecordCommand = new RelayCommand(() => ShowRecordWindow(deviceIndex)); // 添加记录按钮命令

                device.DataChanged += (sender, e) => DeviceDataChanged(deviceIndex, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device);
                Debug.WriteLine($"Loaded device: Id={device.Id}, Name={device.Name}, IP={device.IpAddress}, SlaveId={device.SlaveId}, IsOperational={device.IsOperational}, RunCount={device.RunCount}");
            }
        }

        private void InitializeDefaultDevices()
        {
            for (int i = 0; i < 12; i++)
            {
                string ipAddress;
                int slaveId;
                if (i < 10)
                {
                    ipAddress = "192.168.64.52";
                    slaveId = i + 1;
                }
                else if (i == 10)
                {
                    ipAddress = "192.168.64.87";
                    slaveId = 1;
                }
                else
                {
                    ipAddress = "192.168.64.89";
                    slaveId = 1;
                }

                var device = new DeviceModel
                {
                    Id = i + 1,
                    Name = $"設備 {i + 1}",
                    IpAddress = ipAddress,
                    SlaveId = slaveId,
                    RunCount = 0,
                    Status = "未知",
                    IsOperational = false
                };

                int deviceIndex = i;
                device.StartCommand = new RelayCommand(async () => await ExecuteStart(deviceIndex));
                device.StopCommand = new RelayCommand(async () => await ExecuteStop(deviceIndex));
                device.RecordCommand = new RelayCommand(() => ShowRecordWindow(deviceIndex)); // 添加记录按钮命令

                device.DataChanged += (sender, e) => DeviceDataChanged(deviceIndex, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device);
                _dataService.SaveDeviceData(i, device.Name, device.IpAddress, device.SlaveId, device.IsOperational, device.RunCount);
                Debug.WriteLine($"Initialized default device {i + 1}: Id={device.Id}, Name={device.Name}, IP={device.IpAddress}, SlaveId={device.SlaveId}, IsOperational={device.IsOperational}, RunCount={device.RunCount}");
            }
        }

        [SuppressPropertyChangedWarnings]
        private void DeviceDataChanged(int deviceIndex, string name, string ipAddress, int slaveId, bool isOperational, int runCount)
        {
            var device = Devices[deviceIndex];
            if (device != null)
            {
                device.Name = name;
                device.IpAddress = ipAddress;
                device.SlaveId = slaveId;
                device.IsOperational = isOperational;
                device.RunCount = runCount;
                int dbIndex = device.Id - 1; // 由於資料庫 ID 從 1 開始，轉換為從 0 開始的索引
                _dataService.SaveDeviceData(dbIndex, name, ipAddress, slaveId, isOperational, runCount);
                Debug.WriteLine($"Saved changes for device {deviceIndex + 1}: Name={name}, IP={ipAddress}, SlaveId={slaveId}, IsOperational={isOperational}, RunCount={runCount}");
            }
        }

        private async Task UpdateDeviceData()
        {
            foreach (var device in Devices)
            {
                if (!device.IsOperational)
                {
                    //Debug.WriteLine($"Skipping update for device {device.Name}: Device is not operational.");
                    continue;
                }

                try
                {
                    var statusResult = await _communicationService.ReadModbusAsync(device.IpAddress, device.SlaveId, _statusAddress, 1, 3);
                    if (statusResult.Status == "success" && statusResult.Data.Count > 0)
                    {
                        int statusValue = statusResult.Data[0];
                        switch (statusValue)
                        {
                            case 0:
                                device.Status = "閒置";
                                break;
                            case 1:
                                device.Status = "運行中";
                                break;
                            case 2:
                                device.Status = "故障";
                                break;
                            default:
                                device.Status = "未知";
                                break;
                        }
                        Debug.WriteLine($"Device {device.Name} status updated: {device.Status}");
                    }
                    else
                    {
                        device.Status = "通訊失敗";
                        device.IsOperational = false; // 通訊失敗時，自動設為不運作
                        Debug.WriteLine($"Failed to read status for device {device.Name}: {statusResult.Message}");
                    }

                    if (device.IsOperational) // 只有在仍運作時才繼續讀取跑合次數
                    {
                        var runCountResult = await _communicationService.ReadModbusAsync(device.IpAddress, device.SlaveId, _runCountAddress, 2, 3);
                        if (runCountResult.Status == "success" && runCountResult.Data.Count >= 2)
                        {
                            int lowWord = runCountResult.Data[0];
                            int highWord = runCountResult.Data[1];
                            device.RunCount = (highWord << 16) | (lowWord & 0xFFFF);
                            Debug.WriteLine($"Device {device.Name} run count updated: {device.RunCount}");
                        }
                        else
                        {
                            device.Status = "通訊失敗";
                            device.IsOperational = false; // 通訊失敗時，自動設為不運作
                            Debug.WriteLine($"Failed to read run count for device {device.Name}: {runCountResult.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to update device {device.Name}: {ex.Message}");
                    device.Status = "通訊失敗";
                    device.IsOperational = false; // 異常時也設為不運作
                }
            }
        }

        private async Task ExecuteStart(int deviceIndex)
        {
            var device = Devices[deviceIndex];
            if (!device.IsOperational || device.Status == "運行中" || device.Status == "通訊失敗")
            {
                Debug.WriteLine($"Cannot start device {device.Name}: Device is not operational, already running, or communication failed.");
                return;
            }

            device.Status = "啟動中...";
            try
            {
                _updateTimer.Stop();
                Debug.WriteLine("Update timer stopped for ExecuteStart.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 1, 6);
                await UpdateDeviceData();

                // 播报设备启动
                AnnounceDevice(device, StartAnnouncement);
                PlayAudioAnnouncement(StartAnnouncement);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExecuteStart failed: {ex.Message}");
                device.Status = "啟動失敗";
            }
            finally
            {
                _updateTimer.Start();
                Debug.WriteLine("Update timer restarted after ExecuteStart.");
            }
        }

        private async Task ExecuteStop(int deviceIndex)
        {
            var device = Devices[deviceIndex];
            if (!device.IsOperational || device.Status == "閒置" || device.Status == "通訊失敗")
            {
                Debug.WriteLine($"Cannot stop device {device.Name}: Device is not operational, already idle, or communication failed.");
                return;
            }

            device.Status = "停止中...";
            try
            {
                _updateTimer.Stop();
                Debug.WriteLine("Update timer stopped for ExecuteStop.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 0, 6);
                await UpdateDeviceData();

                // 播报设备停止
                AnnounceDevice(device, StopAnnouncement);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExecuteStop failed: {ex.Message}");
                device.Status = "停止失敗";
            }
            finally
            {
                _updateTimer.Start();
                Debug.WriteLine("Update timer restarted after ExecuteStop.");
            }
        }

        public void StopPolling()
        {
            _updateTimer.Stop();
            _communicationService.CleanupConnections();
            Debug.WriteLine("Update timer stopped and connections cleaned up due to page unload.");
        }

        public async void StartPolling()
        {
            if (!CanControlDevice)
            {
                Debug.WriteLine("StartPolling skipped: User lacks ControlDevice permission.");
                return;
            }

            Debug.WriteLine("StartPolling initiated due to page load.");
            await UpdateDeviceData();
            if (!_updateTimer.Enabled)
            {
                _updateTimer.Start();
                Debug.WriteLine("Update timer started due to page load.");
            }
        }

        private void ShowRecordWindow(int deviceIndex)
        {
            try
            {
                if (deviceIndex < 0 || deviceIndex >= Devices.Count)
                {
                    Debug.WriteLine($"無效的設備索引: {deviceIndex}");
                    MessageBox.Show("請選擇有效的設備", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var device = Devices[deviceIndex];

                // 確保 device.Id 大於 0，這是資料庫中的 ID，不是集合索引
                if (device.Id <= 0)
                {
                    Debug.WriteLine($"無效的設備 ID: {device.Id}，設備索引: {deviceIndex}");
                    MessageBox.Show($"設備 '{device.Name}' 的 ID 無效。請確保設備在資料庫中已正確設置 ID。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var currentUser = _permissionService.CurrentUser?.Username ?? "DefaultUser";
                Debug.WriteLine(
                    $"ShowRecordWindow: " +
                    $"DeviceIndex={deviceIndex}, " +
                    $"DeviceId={device.Id}, " +
                    $"DeviceName={device.Name}, " +
                    $"Username={currentUser} ," +
                    $"Runcount={device.RunCount}"
                    );

                var (deviceId, deviceName, username, runcount) = _recorddialogService.ShowRecordDialog(device.Id, device.Name, currentUser,device.RunCount);
                Debug.WriteLine($"記錄視窗返回: 設備名稱={deviceName}, 使用者名稱={username}, 設備ID={deviceId}, 跑和={runcount}" );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"記錄視窗錯誤: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"顯示記錄視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
