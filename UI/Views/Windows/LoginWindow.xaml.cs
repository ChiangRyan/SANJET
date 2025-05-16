using Microsoft.Extensions.Logging;
using SANJET.Core.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SANJET.UI.Views.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly ILogger<LoginWindow> _logger;

        public string? Username => (DataContext as LoginViewModel)?.Username;
        public string? Password => (DataContext as LoginViewModel)?.Password;

        public LoginWindow(LoginViewModel viewModel, ILogger<LoginWindow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeComponent();
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger.LogInformation("LoginWindow initialized.");
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is PasswordBox passwordBox && DataContext is LoginViewModel viewModel)
                {
                    viewModel.Password = passwordBox.Password ?? string.Empty;
                    _logger.LogDebug("PasswordBox updated in LoginWindow.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update PasswordBox.");
            }
        }
    }
}