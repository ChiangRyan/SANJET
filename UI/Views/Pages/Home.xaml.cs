using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SANJET.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace SANJET.UI.Views.Pages
{
    public partial class Home : Page
    {
        private readonly HomeViewModel _viewModel;
        private readonly ILogger<Home> _logger;

        public Home(IServiceProvider serviceProvider, ILogger<Home> logger)
        {
            InitializeComponent();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = serviceProvider.GetRequiredService<HomeViewModel>()
                ?? throw new ArgumentNullException(nameof(serviceProvider));
            DataContext = _viewModel;

            Loaded += Home_Loaded;
            Unloaded += Home_Unloaded;

            _logger.LogInformation("Home page initialized.");
        }

        private async void Home_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.StartPollingAsync();
            _logger.LogInformation("Update timer started due to page load.");
        }

        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StopPolling();
            _logger.LogInformation("Update timer stopped due to page unload.");
        }
    }
}