using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SANJET.Core.ViewModels;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;

namespace SANJET.UI.Views.Windows
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly ILogger<MainWindow> _logger;
        private readonly IServiceProvider _serviceProvider;

        public Frame MainFrame { get; private set; }

        public MainWindow(IServiceProvider serviceProvider, MainWindowViewModel viewModel, ILogger<MainWindow> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Creating MainWindow...");
            InitializeComponent();
            MainFrame = this.MainContentFrame;
            DataContext = _viewModel;
            _logger.LogInformation("MainWindow DataContext set to: {ViewModelType}", _viewModel.GetType().Name);

            Loaded += MainWindow_Loaded;
            _logger.LogInformation("MainWindow created.");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_viewModel.IsLoggedIn)
                {
                    _logger.LogInformation("Showing LoginWindow from MainWindow.Loaded");
                    _viewModel.ShowLogin();
                }
                await _viewModel.StartPollingAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MainWindow.");
                MessageBox.Show($"初始化失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}