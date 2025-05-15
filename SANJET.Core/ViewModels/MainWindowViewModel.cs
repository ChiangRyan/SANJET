using SANJET.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;
using System.Windows;
using SANJET.SANJET.Core.Services;
using SANJET.SANJET.Core.Interfaces;


namespace SANJET.SANJET.Core.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly ILoginDialogService _loginDialogService;
        private readonly PermissionService _permissionService;
        private readonly Frame _mainFrame;

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        public bool IsLoggedIn => _permissionService.IsLoggedIn;
        public bool CanLogin => !IsLoggedIn;
        public bool CanLogout => IsLoggedIn;

        public bool CanViewHome => _permissionService.HasPermission(Permission.ViewHome);
        public bool CanViewManualOperation => _permissionService.HasPermission(Permission.ViewManualOperation);
        public bool CanViewMonitor => _permissionService.HasPermission(Permission.ViewMonitor);
        public bool CanViewWarning => _permissionService.HasPermission(Permission.ViewWarning);
        public bool CanViewSettings => _permissionService.HasPermission(Permission.ViewSettings);
        public bool CanControlDevice => _permissionService.HasPermission(Permission.ControlDevice);
        public bool CanAll => _permissionService.HasPermission(Permission.All);

        public MainWindowViewModel(PermissionService permissionService, Frame mainFrame, ILoginDialogService dialogService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _mainFrame = mainFrame ?? throw new ArgumentNullException(nameof(mainFrame));
            _loginDialogService = dialogService;

            _permissionService.PermissionsChanged += (s, e) =>
            {
                UpdatePermissionProperties();
                OnPropertyChanged(nameof(IsLoggedIn));
                OnPropertyChanged(nameof(CanLogin));
                OnPropertyChanged(nameof(CanLogout));
                System.Diagnostics.Debug.WriteLine("PermissionsChanged event received.");
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
                    System.Diagnostics.Debug.WriteLine($"Login successful, user: {_permissionService.CurrentUser.Username}");
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
                MessageBox.Show($"登入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password))
                    ShowLogin();
            }
        }

        [RelayCommand(CanExecute = nameof(CanLogout))]
        private void Logout()
        {
            _permissionService.Logout();
            _mainFrame.Navigate(null);
            UpdatePermissionProperties();
            OnPropertyChanged(nameof(IsLoggedIn));
            OnPropertyChanged(nameof(CanLogin));
            OnPropertyChanged(nameof(CanLogout));
            _loginDialogService.ClearNavigationSelection();
            System.Diagnostics.Debug.WriteLine("Logout successful.");
        }

        [RelayCommand]
        private void Navigate(string viewUri)
        {
            if (string.IsNullOrEmpty(viewUri)) return;
            try
            {
                _mainFrame.Navigate(new Uri(viewUri, UriKind.Relative));
                System.Diagnostics.Debug.WriteLine($"Navigated to: {viewUri}");
            }
            catch (Exception ex)
            {
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
                    Username = username;
                    Password = password;
                    Login();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"登入窗口錯誤: {ex.Message}");
            }
        }

        private void UpdatePermissionProperties()
        {
            OnPropertyChanged(nameof(CanViewHome));
            OnPropertyChanged(nameof(CanViewManualOperation));
            OnPropertyChanged(nameof(CanViewMonitor));
            OnPropertyChanged(nameof(CanViewWarning));
            OnPropertyChanged(nameof(CanViewSettings));
            OnPropertyChanged(nameof(CanControlDevice));
            OnPropertyChanged(nameof(CanAll));
            System.Diagnostics.Debug.WriteLine("Permission properties updated.");
        }

        public void NotifyPermissionsChanged()
        {
            UpdatePermissionProperties();
            System.Diagnostics.Debug.WriteLine("NotifyPermissionsChanged called.");
        }
    }
}
