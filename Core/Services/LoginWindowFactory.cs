using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SANJET.Core.Interfaces;
using SANJET.Core.ViewModels;
using SANJET.UI.Views.Windows;
using System;

namespace SANJET.Core.Services
{
    public class LoginWindowFactory : ILoginWindowFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LoginWindow> _logger;

        public LoginWindowFactory(IServiceProvider serviceProvider, ILogger<LoginWindow> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LoginWindow Create()
        {
            var loginViewModel = _serviceProvider.GetRequiredService<LoginViewModel>();
            return new LoginWindow(loginViewModel, _logger);
        }
    }
}