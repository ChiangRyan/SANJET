using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SANJET.Core.Constants.Enums;
using SANJET.Core.Interfaces;
using SANJET.Core.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SANJET.UI.Views.Pages;

namespace SANJET.Core.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILoginDialogService _loginDialogService;
        private readonly PermissionService _permissionService;
        private readonly Frame _mainFrame;
        private readonly HomeViewModel _homeViewModel;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _isHomeSelected;

        public bool IsLoggedIn => _permissionService.IsLoggedIn;
        public bool CanLogin => !IsLoggedIn;
        public bool CanLogout => IsLoggedIn;

        public bool CanViewHome => _permissionService.HasPermission(Permission.ViewHome);
        public bool CanControlDevice => _permissionService.HasPermission(Permission.ControlDevice);
        public bool CanAll => _permissionService.HasPermission(Permission.All);

        public MainWindowViewModel(
            PermissionService permissionService,
            Frame mainFrame,
            ILoginDialogService dialogService,
            HomeViewModel homeViewModel,
            ILogger<MainWindowViewModel> logger,
            IServiceProvider serviceProvider)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _mainFrame = mainFrame ?? throw new ArgumentNullException(nameof(mainFrame));
            _loginDialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _homeViewModel = homeViewModel ?? throw new ArgumentNullException(nameof(homeViewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _permissionService.PermissionsChanged += (s, e) =>
            {
                UpdatePermissionProperties();
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(CanLogin));
                OnPropertyChanged(nameof(CanLogout));
                _logger.LogInformation("PermissionsChanged event received.");
            };
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private void Login()
        {
            try
            {
                if (_permissionService.Login(Username, Password))
                {
                    UpdatePermissionProperties();
                    OnPropertyChanged(nameof(IsLoggedIn));
                    OnPropertyChanged(nameof(CanLogin));
                    OnPropertyChanged(nameof(CanLogout));
                    Username = string.Empty;
                    Password = string.Empty;
                    _logger.LogInformation("Login successful, user: {Username}", _permissionService.CurrentUser?.Username);
                }
                else
                {
                    MessageBox.Show("登入失敗：使用者名稱或密碼錯誤。", "登入錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
                        ShowLogin();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed.");
                MessageBox.Show($"登入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
                    ShowLogin();
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogout))]
        private void Logout()
        {
            try
            {
                _permissionService.Logout();
                ClearNavigationSelection();
                UpdatePermissionProperties();
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(CanLogin));
                OnPropertyChanged(nameof(CanLogout));
                _logger.LogInformation("Logout successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed.");
                MessageBox.Show($"登出失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ClearNavigationSelection()
        {
            try
            {
                _mainFrame.Navigate(null);
                IsHomeSelected = false;
                _logger.LogInformation("Navigation selection cleared.");
                OnPropertyChanged(nameof(CanViewHome));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear navigation selection.");
            }
        }

        [RelayCommand]
        private void NavigateHome()
        {
            try
            {
                var homePage = _serviceProvider.GetRequiredService<Home>();
                _mainFrame.Navigate(homePage);
                IsHomeSelected = true;
                _logger.LogInformation("Navigated to Home page.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to navigate to Home page.");
                MessageBox.Show($"導航失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void ShowLogin()
        {
            try
            {
                var (success, username, password) = _loginDialogService.ShowLoginDialog();
                if (success)
                {
                    Username = username ?? string.Empty; // Ensure non-null assignment
                    Password = password ?? string.Empty; // Ensure non-null assignment
                    Login();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Show login dialog failed.");
                MessageBox.Show($"顯示登入視窗失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*
        [RelayCommand]
        private async Task StartAll()
        {
            try
            {
                _logger.LogInformation("Starting all devices...");
                await _homeViewModel.StartAllDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start all devices.");
                MessageBox.Show($"啟動所有設備失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task StopAll()
        {
            try
            {
                _logger.LogInformation("Stopping all devices...");
                await _homeViewModel.StopAllDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop all devices.");
                MessageBox.Show($"停止所有設備失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        */



        private void UpdatePermissionProperties()
        {
            OnPropertyChanged(nameof(CanViewHome));
            OnPropertyChanged(nameof(CanControlDevice));
            OnPropertyChanged(nameof(CanAll));
            _logger.LogInformation("Permission properties updated.");
        }

        public void NotifyPermissionsChanged()
        {
            UpdatePermissionProperties();
            _logger.LogInformation("NotifyPermissionsChanged called.");
        }
    }
}