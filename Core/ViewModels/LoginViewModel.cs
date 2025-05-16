using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;

namespace SANJET.Core.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly Window _window;
        private readonly ILogger<LoginViewModel> _logger;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        public LoginViewModel(Window window, ILogger<LoginViewModel> logger)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("LoginViewModel initialized.");
        }

        [RelayCommand]
        private void Login()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    _logger.LogWarning("Login attempt failed: Username or Password is empty.");
                    MessageBox.Show("請輸入使用者名稱和密碼。", "輸入錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _logger.LogInformation("Login command executed. Username: {Username}", Username);
                _window.DialogResult = true;
                _window.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login command failed.");
                MessageBox.Show($"登入失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            try
            {
                _logger.LogInformation("Cancel command executed.");
                _window.DialogResult = false;
                _window.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel command failed.");
                MessageBox.Show($"取消失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}