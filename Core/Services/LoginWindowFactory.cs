using Microsoft.Extensions.Logging;
using SANJET.Core.Interfaces;
using SANJET.Core.ViewModels;
using SANJET.UI.Views.Windows;

namespace SANJET.Core.Services
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        private readonly LoginViewModel _loginViewModel;
        private readonly ILogger<LoginWindow> _logger;

        public LoginWindowFactory(LoginViewModel loginViewModel, ILogger<LoginWindow> logger)
        {
            _loginViewModel = loginViewModel ?? throw new ArgumentNullException(nameof(loginViewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LoginWindow Create()
        {
            return new LoginWindow(_loginViewModel, _logger);
        }
    }
}