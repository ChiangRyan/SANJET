using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SANJET.Core.Constants.Enums;
using SANJET.Core.Interfaces;
using SANJET.Core.Models; // 確保 DeviceModel 在這裡
using SANJET.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;


using System.Windows; // For MessageBox

namespace SANJET.Core.ViewModels
{
    public partial class HomeViewModel : ObservableObject // 1. 繼承 ObservableObject
    {
        private readonly ICommunicationService _communicationService;
        private readonly SqliteDataService _dataService;
        private readonly PermissionService _permissionService;
        private readonly IRecordDialogService _recorddialogService;
        private readonly ITextToSpeechService _textToSpeechService;
        private readonly IAudioPlayerService _audioPlayerService;

        private readonly System.Timers.Timer _updateTimer; //明確指定 Timer 的命名空間

        // Modbus 位置設定
        private readonly int _runCountAddress = 10;
        private readonly int _statusAddress = 1;
        private readonly int _controlAddress = 0;

        // 2. 使用 [ObservableProperty]
        [ObservableProperty]
        private ObservableCollection<DeviceModel> _devices;

        [ObservableProperty]
        private string _startAnnouncement = "啟動中，請注意安全";

        [ObservableProperty]
        private string _stopAnnouncement = "停止中，請注意安全";

        [ObservableProperty]
        private bool _enableVoiceAnnouncement = true;

        // CanControlDevice 屬性依賴於 PermissionService 的事件
        public bool CanControlDevice => _permissionService.HasPermission(Permission.ControlDevice);

        public HomeViewModel(
            ICommunicationService communicationService,
            SqliteDataService dataService,
            IRecordDialogService recordDialogService,
            PermissionService permissionService,
            ITextToSpeechService textToSpeechService,
            IAudioPlayerService audioPlayerService)
        {
            _communicationService = communicationService;
            _dataService = dataService;
            _recorddialogService = recordDialogService;
            _permissionService = permissionService;
            _textToSpeechService = textToSpeechService;
            _audioPlayerService = audioPlayerService;

            // 設置語音速度
            _textToSpeechService.Rate = -1;

            // 訂閱權限變更事件
            _permissionService.PermissionsChanged += OnPermissionsChanged;

            _devices = new ObservableCollection<DeviceModel>(); // 初始化 ObservableProperty 的後備欄位

            // 先從資料庫載入設備數據
            LoadDevicesFromDatabase();

            // 如果資料庫中沒有設備數據，則初始化預設設備
            if (!Devices.Any()) // Devices 屬性由 [ObservableProperty] 生成
            {
                InitializeDefaultDevices();
            }

            _updateTimer = new System.Timers.Timer(5000);
            _updateTimer.Elapsed += async (s, e) => await UpdateDeviceDataAsync(); // 方法名加上 Async 後綴以示區別
            _updateTimer.AutoReset = true;
        }

        private void OnPermissionsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanControlDevice)); // 手動通知 CanControlDevice 變更
            Debug.WriteLine($"PermissionsChanged event triggered: CanControlDevice={CanControlDevice}");
            // 如果失去控制權限，停止輪詢
            if (!CanControlDevice)
            {
                StopPolling();
                Debug.WriteLine("Polling stopped due to lack of ControlDevice permission.");
            }
        }

        private void AnnounceDevice(DeviceModel device, string actionMessage)
        {
            if (!EnableVoiceAnnouncement) return;

            try
            {
                string announcement = $"設備 {device.Name} {actionMessage}";
                _textToSpeechService.Speak(announcement);
                Debug.WriteLine($"语音播报: {announcement}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"语音播报错误: {ex.Message}");
            }
        }

        private void PlayAudioAnnouncement(string audioFileNameKey) // 參數可以是檔案名或代表動作的鍵
        {
            if (!EnableVoiceAnnouncement) return;
            try
            {
                // 假設 audioFileNameKey 為 "Start" 或 "Stop" 等，對應到具體的 mp3 檔案
                // 您可能需要一個機制來從 key 映射到檔案路徑
                string audioFileName = "SquidGame1.mp3"; // 範例檔案名
                if (audioFileNameKey == StopAnnouncement) // 簡單示例，您可能需要更複雜的邏輯
                {
                    // audioFileName = "another_sound.mp3"; // 例如停止時播放不同音效
                }

                // 使用 AppContext.BaseDirectory 替代 AppDomain.CurrentDomain.BaseDirectory in .NET Core/.NET 5+
                string audioFilePath = Path.Combine(AppContext.BaseDirectory, "Audio", audioFileName); // 假設音檔在 Audio 子目錄
                if (!File.Exists(audioFilePath))
                {
                    Debug.WriteLine($"Audio file not found: {audioFilePath}");
                    // 嘗試在根目錄尋找 (相容舊路徑)
                    audioFilePath = Path.Combine(AppContext.BaseDirectory, audioFileName);
                }


                if (File.Exists(audioFilePath))
                {
                    _audioPlayerService.Play(audioFilePath);
                    Debug.WriteLine($"音频播报: {audioFilePath}");
                }
                else
                {
                    Debug.WriteLine($"Audio file still not found after checking multiple paths: {audioFileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"音频播报错误: {ex.Message}");
            }
        }

        private void LoadDevicesFromDatabase()
        {
            var deviceDataList = _dataService.GetDeviceData();
            foreach (var data in deviceDataList)
            {
                string ipAddress;
                int slaveId;
                int index = deviceDataList.IndexOf(data); // 獲取當前 data 的索引

                if (index < 10)
                {
                    ipAddress = "192.168.64.52";
                    slaveId = index + 1;
                }
                else if (index == 10)
                {
                    ipAddress = "192.168.64.87";
                    slaveId = 1;
                }
                else // index == 11 (or more, if device count changes)
                {
                    ipAddress = "192.168.64.89";
                    slaveId = 1;
                }

                var device = new DeviceModel
                {
                    Id = data.Id,
                    Name = data.Name ?? $"設備 {index + 1}",
                    IpAddress = data.IpAddress ?? ipAddress,
                    SlaveId = data.SlaveId > 0 ? data.SlaveId : slaveId,
                    RunCount = data.RunCount,
                    Status = "未知",
                    IsOperational = data.IsOperational
                };

                // 3. 使用 MVVM Toolkit 的 RelayCommand/AsyncRelayCommand
                // 由於 ExecuteStartAsync 等方法現在是 HomeViewModel 的一部分 (透過 [RelayCommand] or similar)
                // 或者它們是私有方法被這裡的 Lambda 調用。
                // 當命令在 DeviceModel 上時，我們仍需手動創建它們。
                // 這裡的 deviceIndex 就是上面獲取的 index
                device.StartCommand = new AsyncRelayCommand(async () => await ExecuteStartDeviceAsync(device));
                device.StopCommand = new AsyncRelayCommand(async () => await ExecuteStopDeviceAsync(device));
                device.RecordCommand = new RelayCommand(() => ShowRecordWindowForDevice(device));

                device.DataChanged += (sender, e) => DeviceDataChanged(device, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device); // Devices 是 [ObservableProperty] 的欄位 _devices 的公開屬性
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
                else // i == 11
                {
                    ipAddress = "192.168.64.89";
                    slaveId = 1;
                }

                var device = new DeviceModel
                {
                    Id = i + 1, // 假設 Id 從 1 開始
                    Name = $"設備 {i + 1}",
                    IpAddress = ipAddress,
                    SlaveId = slaveId,
                    RunCount = 0,
                    Status = "未知",
                    IsOperational = false // 預設為 false，讓使用者手動啟用
                };

                device.StartCommand = new AsyncRelayCommand(async () => await ExecuteStartDeviceAsync(device));
                device.StopCommand = new AsyncRelayCommand(async () => await ExecuteStopDeviceAsync(device));
                device.RecordCommand = new RelayCommand(() => ShowRecordWindowForDevice(device));

                device.DataChanged += (sender, e) => DeviceDataChanged(device, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device);
                // 儲存到資料庫時，Id 通常由資料庫生成 (如果 Id 是主鍵且自增長)
                // 或者如果我們自己管理 Id，要確保與 DeviceModel.Id 一致
                // SaveDeviceData 的第一個參數是 deviceIndex (0-based)，但 DeviceModel.Id 是 1-based
                _dataService.SaveDeviceData(device.Id - 1, device.Name, device.IpAddress, device.SlaveId, device.IsOperational, device.RunCount);
                Debug.WriteLine($"Initialized default device {i + 1}: Id={device.Id}, Name={device.Name}, IP={device.IpAddress}, SlaveId={device.SlaveId}, IsOperational={device.IsOperational}, RunCount={device.RunCount}");
            }
        }

        // 參數改為 DeviceModel，移除 [SuppressPropertyChangedWarnings] (Fody)
        private void DeviceDataChanged(DeviceModel device, string name, string ipAddress, int slaveId, bool isOperational, int runCount)
        {
            if (device != null)
            {
                // 更新 DeviceModel 的屬性 (如果它們還沒被 DataChanged 事件的觸發源更新的話)
                // 實際上，DeviceModel 的 setter 應該已經觸發了 DataChanged，所以這裡主要是為了儲存
                // device.Name = name; // 通常 DataChanged 事件發生在屬性已改變之後
                // device.IpAddress = ipAddress;
                // device.SlaveId = slaveId;
                // device.IsOperational = isOperational;
                // device.RunCount = runCount;

                // 確保使用正確的 Id (通常是 device.Id - 1 如果 SaveDeviceData 的 deviceIndex 是 0-based)
                _dataService.SaveDeviceData(device.Id - 1, name, ipAddress, slaveId, isOperational, runCount);
                Debug.WriteLine($"Saved changes for device {device.Name} (ID: {device.Id}): Name={name}, IP={ipAddress}, SlaveId={slaveId}, IsOperational={isOperational}, RunCount={runCount}");
            }
        }

        private async Task UpdateDeviceDataAsync()
        {
            foreach (var device in Devices)
            {
                if (!device.IsOperational)
                {
                    continue;
                }

                try
                {
                    var statusResult = await _communicationService.ReadModbusAsync(device.IpAddress, device.SlaveId, _statusAddress, 1, 3);
                    if (statusResult.Status == "success" && statusResult.Data.Any())
                    {
                        int statusValue = statusResult.Data[0];
                        device.Status = statusValue switch
                        {
                            0 => "閒置",
                            1 => "運行中",
                            2 => "故障",
                            _ => "未知"
                        };
                        Debug.WriteLine($"Device {device.Name} status updated: {device.Status}");
                    }
                    else
                    {
                        device.Status = "通訊失敗";
                        device.IsOperational = false; // 通訊失敗時，自動設為不運作
                        Debug.WriteLine($"Failed to read status for device {device.Name}: {statusResult.Message}");
                    }

                    if (device.IsOperational)
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
                            device.IsOperational = false;
                            Debug.WriteLine($"Failed to read run count for device {device.Name}: {runCountResult.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to update device {device.Name}: {ex.Message}");
                    device.Status = "通訊失敗";
                    device.IsOperational = false;
                }
            }
        }

        // 改為接收 DeviceModel 參數，以便於從 UI 綁定 CommandParameter
        private async Task ExecuteStartDeviceAsync(DeviceModel device)
        {
            if (device == null || !device.IsOperational || device.Status == "運行中" || device.Status == "通訊失敗")
            {
                Debug.WriteLine($"Cannot start device {(device?.Name) ?? "N/A"}: Device is not operational, already running, or communication failed.");
                return;
            }

            device.Status = "啟動中...";
            try
            {
                _updateTimer.Stop();
                Debug.WriteLine("Update timer stopped for ExecuteStartDeviceAsync.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 1, 6);
                await UpdateDeviceDataAsync(); // 立即更新一次狀態

                AnnounceDevice(device, StartAnnouncement); // StartAnnouncement 是 [ObservableProperty]
                PlayAudioAnnouncement(StartAnnouncement); // 傳遞播報文本作為識別鍵
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExecuteStartDeviceAsync for {device.Name} failed: {ex.Message}");
                device.Status = "啟動失敗";
            }
            finally
            {
                if (CanControlDevice) // 只有在仍有權限時才重啟 timer
                {
                    _updateTimer.Start();
                    Debug.WriteLine("Update timer restarted after ExecuteStartDeviceAsync.");
                }
            }
        }

        private async Task ExecuteStopDeviceAsync(DeviceModel device)
        {
            if (device == null || !device.IsOperational || device.Status == "閒置" || device.Status == "通訊失敗")
            {
                Debug.WriteLine($"Cannot stop device {(device?.Name) ?? "N/A"}: Device is not operational, already idle, or communication failed.");
                return;
            }

            device.Status = "停止中...";
            try
            {
                _updateTimer.Stop();
                Debug.WriteLine("Update timer stopped for ExecuteStopDeviceAsync.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 0, 6);
                await UpdateDeviceDataAsync(); // 立即更新一次狀態

                AnnounceDevice(device, StopAnnouncement); // StopAnnouncement 是 [ObservableProperty]
                // PlayAudioAnnouncement(StopAnnouncement); // 可選：停止時也播放音效
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ExecuteStopDeviceAsync for {device.Name} failed: {ex.Message}");
                device.Status = "停止失敗";
            }
            finally
            {
                if (CanControlDevice)
                {
                    _updateTimer.Start();
                    Debug.WriteLine("Update timer restarted after ExecuteStopDeviceAsync.");
                }
            }
        }

        public void StopPolling() // 這個方法由外部 (例如頁面 Unloaded 事件) 調用
        {
            _updateTimer.Stop();
            _communicationService.CleanupConnections(); // 假設 ICommunicationService 有此方法
            Debug.WriteLine("Update timer stopped and connections cleaned up.");
        }

        public async void StartPolling() // 這個方法由外部 (例如頁面 Loaded 事件) 調用
        {
            if (!CanControlDevice)
            {
                Debug.WriteLine("StartPolling skipped: User lacks ControlDevice permission.");
                return;
            }

            Debug.WriteLine("StartPolling initiated.");
            await UpdateDeviceDataAsync();
            if (!_updateTimer.Enabled) // 檢查 Timer 是否已啟用
            {
                _updateTimer.Start();
                Debug.WriteLine("Update timer started.");
            }
        }

        private void ShowRecordWindowForDevice(DeviceModel device)
        {
            if (device == null)
            {
                Debug.WriteLine($"無效的設備實例");
                MessageBox.Show("請選擇有效的設備", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                if (device.Id <= 0)
                {
                    Debug.WriteLine($"無效的設備 ID: {device.Id}，設備名稱: {device.Name}");
                    MessageBox.Show($"設備 '{device.Name}' 的 ID 無效。請確保設備在資料庫中已正確設置 ID。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var currentUser = _permissionService.CurrentUser?.Username ?? "DefaultUser";
                Debug.WriteLine(
                    $"ShowRecordWindowForDevice: " +
                    $"DeviceId={device.Id}, " +
                    $"DeviceName={device.Name}, " +
                    $"Username={currentUser}, " +
                    $"Runcount={device.RunCount}");

                // IRecordDialogService 的 ShowRecordDialog 應返回一個 Tuple 或特定結果對象
                var dialogResult = _recorddialogService.ShowRecordDialog(device.Id, device.Name, currentUser, device.RunCount);
                // 根據 dialogResult 處理後續邏輯 (如果需要)
                Debug.WriteLine($"記錄視窗調用完成，設備名稱={dialogResult.deviceName}, 使用者名稱={dialogResult.username}, 設備ID={dialogResult.deviceId}, 跑和={dialogResult.runcount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"記錄視窗錯誤: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show($"顯示記錄視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}