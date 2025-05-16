using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SANJET.Core.Constants.Enums;
using SANJET.Core.Interfaces;
using SANJET.Core.Models;
using SANJET.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace SANJET.Core.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly ICommunicationService _communicationService;
        private readonly SqliteDataService _dataService;
        private readonly PermissionService _permissionService;
        private readonly IRecordDialogService _recorddialogService;
        private readonly ITextToSpeechService _textToSpeechService;
        private readonly IAudioPlayerService _audioPlayerService;
        private readonly ILogger<HomeViewModel> _logger;
        private readonly System.Timers.Timer _updateTimer;
        private readonly Dictionary<string, string> _audioFiles = new()
        {
            { "StartAnnouncement", "SquidGame1.mp3" },
            { "StopAnnouncement", "StopSound.mp3" }
        };

        private readonly int _runCountAddress = 10;
        private readonly int _statusAddress = 1;
        private readonly int _controlAddress = 0;

        [ObservableProperty]
        private ObservableCollection<DeviceModel> _devices;

        [ObservableProperty]
        private string _startAnnouncement = "啟動中，請注意安全";

        [ObservableProperty]
        private string _stopAnnouncement = "停止中，請注意安全";

        [ObservableProperty]
        private bool _enableVoiceAnnouncement = true;

        public bool CanControlDevice => _permissionService.HasPermission(Permission.ControlDevice);

        public HomeViewModel(
            ICommunicationService communicationService,
            SqliteDataService dataService,
            IRecordDialogService recordDialogService,
            PermissionService permissionService,
            ITextToSpeechService textToSpeechService,
            IAudioPlayerService audioPlayerService,
            ILogger<HomeViewModel> logger)
        {
            _communicationService = communicationService;
            _dataService = dataService;
            _recorddialogService = recordDialogService;
            _permissionService = permissionService;
            _textToSpeechService = textToSpeechService;
            _audioPlayerService = audioPlayerService;
            _logger = logger;

            _textToSpeechService.Rate = -1;
            _permissionService.PermissionsChanged += OnPermissionsChanged;

            _devices = new ObservableCollection<DeviceModel>();
            LoadDevicesFromDatabase();
            if (!Devices.Any())
            {
                InitializeDefaultDevices();
            }

            _updateTimer = new System.Timers.Timer(5000);
            _updateTimer.Elapsed += async (s, e) => await UpdateDeviceDataAsync();
            _updateTimer.AutoReset = true;
        }

        private void OnPermissionsChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanControlDevice));
            _logger.LogInformation("PermissionsChanged event triggered: CanControlDevice={CanControlDevice}", CanControlDevice);
            if (!CanControlDevice)
            {
                StopPolling();
                _logger.LogInformation("Polling stopped due to lack of ControlDevice permission.");
            }
        }

        private void AnnounceDevice(DeviceModel device, string actionMessage)
        {
            if (!EnableVoiceAnnouncement) return;

            try
            {
                string announcement = $"設備 {device.Name} {actionMessage}";
                _textToSpeechService.Speak(announcement);
                _logger.LogInformation("Speech announcement: {Announcement}", announcement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech announcement failed.");
            }
        }

        private void PlayAudioAnnouncement(string audioFileNameKey)
        {
            if (!EnableVoiceAnnouncement) return;

            try
            {
                if (!_audioFiles.TryGetValue(audioFileNameKey, out var audioFileName))
                {
                    _logger.LogWarning("No audio file mapped for key: {Key}", audioFileNameKey);
                    return;
                }

                string audioFilePath = Path.Combine(AppContext.BaseDirectory, "Audio", audioFileName);
                if (!File.Exists(audioFilePath))
                {
                    audioFilePath = Path.Combine(AppContext.BaseDirectory, audioFileName);
                }

                if (File.Exists(audioFilePath))
                {
                    _audioPlayerService.Play(audioFilePath);
                    _logger.LogInformation("Audio played: {AudioFilePath}", audioFilePath);
                }
                else
                {
                    _logger.LogWarning("Audio file not found: {AudioFilePath}", audioFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play audio for key: {Key}", audioFileNameKey);
            }
        }

        private void LoadDevicesFromDatabase()
        {
            var deviceDataList = _dataService.GetDeviceData();
            foreach (var data in deviceDataList)
            {
                string ipAddress;
                int slaveId;
                int index = deviceDataList.IndexOf(data);

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
                else
                {
                    ipAddress = "192.168.64.89";
                    slaveId = 1;
                }

                var device = new DeviceModel(
                    id: data.Id,
                    initialName: data.Name ?? $"設備 {index + 1}",
                    initialIpAddress: data.IpAddress ?? ipAddress,
                    initialSlaveId: data.SlaveId > 0 ? data.SlaveId : slaveId,
                    initialRunCount: data.RunCount,
                    initialStatus: "未知",
                    initialIsOperational: data.IsOperational
                );

                device.StartCommand = StartDeviceCommand;
                device.StopCommand = StopDeviceCommand;
                device.RecordCommand = ShowRecordWindowCommand;

                device.DataChanged += (sender, e) => DeviceDataChanged(device, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device);
                _logger.LogInformation("Loaded device: Id={Id}, Name={Name}, IP={IpAddress}, SlaveId={SlaveId}, IsOperational={IsOperational}, RunCount={RunCount}",
                    device.Id, device.Name, device.IpAddress, device.SlaveId, device.IsOperational, device.RunCount);
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

                var device = new DeviceModel(
                    id: i + 1,
                    initialName: $"設備 {i + 1}",
                    initialIpAddress: ipAddress,
                    initialSlaveId: slaveId,
                    initialRunCount: 0,
                    initialStatus: "未知",
                    initialIsOperational: false
                );

                device.StartCommand = StartDeviceCommand;
                device.StopCommand = StopDeviceCommand;
                device.RecordCommand = ShowRecordWindowCommand;

                device.DataChanged += (sender, e) => DeviceDataChanged(device, e.Name, e.IpAddress, e.SlaveId, e.IsOperational, e.RunCount);

                Devices.Add(device);
                _dataService.SaveDeviceData(device.Id, device.Name, device.IpAddress, device.SlaveId, device.IsOperational, device.RunCount);
                _logger.LogInformation("Initialized default device {Index}: Id={Id}, Name={Name}, IP={IpAddress}, SlaveId={SlaveId}, IsOperational={IsOperational}, RunCount={RunCount}",
                    i + 1, device.Id, device.Name, device.IpAddress, device.SlaveId, device.IsOperational, device.RunCount);
            }
        }

        private void DeviceDataChanged(DeviceModel device, string? name, string? ipAddress, int slaveId, bool isOperational, int runCount)
        {
            if (device == null)
            {
                _logger.LogWarning("Device is null in DeviceDataChanged.");
                return;
            }

            string safeName = name ?? device.Name ?? $"設備 {device.Id}";
            string safeIpAddress = ipAddress ?? device.IpAddress ?? "Unknown";

            _dataService.SaveDeviceData(device.Id, safeName, safeIpAddress, slaveId, isOperational, runCount);
            _logger.LogInformation("Saved changes for device {Name} (ID: {Id}): Name={SafeName}, IP={SafeIpAddress}, SlaveId={SlaveId}, IsOperational={IsOperational}, RunCount={RunCount}",
                device.Name, device.Id, safeName, safeIpAddress, slaveId, isOperational, runCount);
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
                        _logger.LogInformation("Device {Name} status updated: {Status}", device.Name, device.Status);
                    }
                    else
                    {
                        device.Status = "通訊失敗";
                        device.IsOperational = false;
                        _logger.LogWarning("Failed to read status for device {Name}: {Message}", device.Name, statusResult.Message);
                    }

                    if (device.IsOperational)
                    {
                        var runCountResult = await _communicationService.ReadModbusAsync(device.IpAddress, device.SlaveId, _runCountAddress, 2, 3);
                        if (runCountResult.Status == "success" && runCountResult.Data.Count >= 2)
                        {
                            int lowWord = runCountResult.Data[0];
                            int highWord = runCountResult.Data[1];
                            device.RunCount = (highWord << 16) | (lowWord & 0xFFFF);
                            _logger.LogInformation("Device {Name} run count updated: {RunCount}", device.Name, device.RunCount);
                        }
                        else
                        {
                            device.Status = "通訊失敗";
                            device.IsOperational = false;
                            _logger.LogWarning("Failed to read run count for device {Name}: {Message}", device.Name, runCountResult.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update device {Name}.", device.Name);
                    device.Status = "通訊失敗";
                    device.IsOperational = false;
                }
            }
        }

        [RelayCommand]
        private async Task StartDeviceAsync(DeviceModel device)
        {
            if (device == null || !device.IsOperational || device.Status == "運行中" || device.Status == "通訊失敗")
            {
                _logger.LogWarning("Cannot start device {DeviceName}: Device is not operational, already running, or communication failed.", device?.Name ?? "N/A");
                return;
            }

            device.Status = "啟動中...";
            try
            {
                _updateTimer.Stop();
                _logger.LogInformation("Update timer stopped for StartDeviceAsync.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 1, 6);
                await UpdateDeviceDataAsync();

                AnnounceDevice(device, StartAnnouncement);
                PlayAudioAnnouncement("StartAnnouncement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartDeviceAsync for {DeviceName} failed.", device.Name);
                device.Status = "啟動失敗";
            }
            finally
            {
                if (CanControlDevice)
                {
                    _updateTimer.Start();
                    _logger.LogInformation("Update timer restarted after StartDeviceAsync.");
                }
            }
        }

        [RelayCommand]
        private async Task StopDeviceAsync(DeviceModel device)
        {
            if (device == null || !device.IsOperational || device.Status == "閒置" || device.Status == "通訊失敗")
            {
                _logger.LogWarning("Cannot stop device {DeviceName}: Device is not operational, already idle, or communication failed.", device?.Name ?? "N/A");
                return;
            }

            device.Status = "停止中...";
            try
            {
                _updateTimer.Stop();
                _logger.LogInformation("Update timer stopped for StopDeviceAsync.");
                await _communicationService.WriteModbusAsync(device.IpAddress, device.SlaveId, _controlAddress, 0, 6);
                await UpdateDeviceDataAsync();

                AnnounceDevice(device, StopAnnouncement);
                PlayAudioAnnouncement("StopAnnouncement");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StopDeviceAsync for {DeviceName} failed.", device.Name);
                device.Status = "停止失敗";
            }
            finally
            {
                if (CanControlDevice)
                {
                    _updateTimer.Start();
                    _logger.LogInformation("Update timer restarted after StopDeviceAsync.");
                }
            }
        }

        [RelayCommand]
        private void ShowRecordWindow(DeviceModel device)
        {
            if (device == null)
            {
                _logger.LogWarning("Invalid device instance.");
                MessageBox.Show("請選擇有效的設備", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (device.Id <= 0)
            {
                _logger.LogWarning("Invalid device ID: {Id}, Name: {Name}", device.Id, device.Name);
                MessageBox.Show($"設備 '{device.Name}' 的 ID 無效。請確保設備在資料庫中已正確設置 ID。", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string currentUser = _permissionService.CurrentUser?.Username
                    ?? throw new InvalidOperationException("Current user is not set.");
                _logger.LogInformation("ShowRecordWindowForDevice: DeviceId={DeviceId}, DeviceName={DeviceName}, Username={Username}, Runcount={Runcount}",
                    device.Id, device.Name, currentUser, device.RunCount);

                var dialogResult = _recorddialogService.ShowRecordDialog(device.Id, device.Name, currentUser, device.RunCount);
                _logger.LogInformation("Record dialog completed: DeviceName={DeviceName}, Username={Username}, DeviceId={DeviceId}, Runcount={Runcount}",
                    dialogResult.deviceName, dialogResult.username, dialogResult.deviceId, dialogResult.runcount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Record dialog error.");
                MessageBox.Show($"顯示記錄視窗失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StopPolling()
        {
            _updateTimer.Stop();
            _communicationService.CleanupConnections();
            _logger.LogInformation("Update timer stopped and connections cleaned up.");
        }

        public async Task StartPollingAsync()
        {
            if (!CanControlDevice)
            {
                _logger.LogInformation("StartPolling skipped: User lacks ControlDevice permission.");
                return;
            }

            _logger.LogInformation("StartPolling initiated.");
            await UpdateDeviceDataAsync();
            if (!_updateTimer.Enabled)
            {
                _updateTimer.Start();
                _logger.LogInformation("Update timer started.");
            }
        }
    }
}