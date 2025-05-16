using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SANJET.Core.ViewModels;
using SANJET.UI.Views.Pages;
using Microsoft.Extensions.Logging;
using System.Windows.Controls;

namespace SANJET.UI.Views.Windows
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly ILogger<MainWindow> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Home? _homePage;

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

        private void InitializeHomePage()
        {
            _homePage = _serviceProvider.GetRequiredService<Home>();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsLoggedIn)
            {
                _logger.LogInformation("Showing LoginWindow from MainWindow.Loaded");
                _viewModel.ShowLogin();
            }
            await _viewModel.StartPollingAsync();
        }

        private void HomeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_homePage == null)
            {
                InitializeHomePage();
            }
            MainFrame.Navigate(_homePage);
            _viewModel.IsHomeSelected = true;
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