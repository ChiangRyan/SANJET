using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SANJET.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SANJET.UI.Views.Pages
{
    public partial class Home : Page
    {
        private readonly HomeViewModel _viewModel;
        private readonly ILogger<Home> _logger;
        private bool _isDataLoaded = false;

        public Home(IServiceProvider serviceProvider, ILogger<Home> logger)
        {
            InitializeComponent();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = serviceProvider.GetRequiredService<HomeViewModel>()
                ?? throw new ArgumentNullException(nameof(serviceProvider));
            DataContext = _viewModel;

            Loaded += Home_Loaded_Async;
            Unloaded += Home_Unloaded;

            _logger.LogInformation("Home page initialized. Data will be loaded on Home_Loaded event.");
        }

        private async void Home_Loaded_Async(object sender, RoutedEventArgs e)
        {
            if (!_isDataLoaded)
            {
                _logger.LogInformation("Home page loaded. Starting to load initial data for HomeViewModel...");
                // 確保 LoadInitialDataAsync 在 HomeViewModel 中是 public async Task
                await _viewModel.LoadInitialDataAsync();
                _isDataLoaded = true;
                _logger.LogInformation("HomeViewModel initial data loaded.");

                // 資料載入完成後，再啟動輪詢
                if (_viewModel.CanControlDevice)
                {
                    // StartPollingAsync 內部會判斷 Timer 是否已啟動
                    await _viewModel.StartPollingAsync();
                    // _logger.LogInformation("Update timer started due to page load and data loaded."); // 這行可以移到 StartPollingAsync 內部，或者保留
                }
                else
                {
                    _logger.LogInformation("Update timer not started: User lacks ControlDevice permission.");
                }
            }
            else
            {
                _logger.LogInformation("Home page loaded, but data already loaded. Attempting to ensure polling is active if permitted.");
                if (_viewModel.CanControlDevice)
                {
                    // 即使資料已載入，也確保輪詢被觸發（如果它因為某些原因停止了）
                    await _viewModel.StartPollingAsync();
                }
            }
        }

        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StopPolling();
            _logger.LogInformation("Update timer stopped due to page unload.");
        }
    }
}