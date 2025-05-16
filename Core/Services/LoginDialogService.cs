using Microsoft.Extensions.Logging;
using SANJET.Core.Interfaces;
using SANJET.Core.ViewModels;
using SANJET.UI.Views.Windows;
using System;

namespace SANJET.Core.Services
{
    public class LoginDialogService : ILoginDialogService
    {
        private readonly ILogger<LoginDialogService> _logger;
        private readonly ILoginWindowFactory _loginWindowFactory;

        public LoginDialogService(ILoginWindowFactory loginWindowFactory, ILogger<LoginDialogService> logger)
        {
            _loginWindowFactory = loginWindowFactory ?? throw new ArgumentNullException(nameof(loginWindowFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("LoginDialogService initialized.");
        }

        public (bool Success, string? Username, string? Password) ShowLoginDialog()
        {
            _logger.LogInformation("Showing login dialog.");
            try
            {
                var loginWindow = _loginWindowFactory.Create();
                bool? result = loginWindow.ShowDialog();
                if (result == true)
                {
                    _logger.LogInformation("Login dialog closed with success. Username: {Username}", loginWindow.Username);
                    return (true, loginWindow.Username, loginWindow.Password);
                }
                _logger.LogInformation("Login dialog closed without success.");
                return (false, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show login dialog.");
                return (false, string.Empty, string.Empty);
            }
        }
    }
}