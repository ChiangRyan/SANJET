using System;
using System.Windows;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SANJET.Core.Interfaces;
using SANJET.Core.Services;
using SANJET.Core.ViewModels;
using SANJET.UI.Views.Pages;

namespace SANJET.UI.Views.Windows
{
    public partial class MainWindow : Window, ILoginDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MainWindowViewModel _viewModel; // 宣告為類欄位
        private Home _homePage;

        public MainWindow(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("Creating MainWindow...");
            InitializeComponent();

            // 從 App.xaml.cs 獲取 IServiceProvider
            _serviceProvider = (App.Current as App)?.ServiceProvider;
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider is not initialized.");
            }

            var permissionService = _serviceProvider.GetService<PermissionService>();
            if (permissionService == null)
            {
                throw new InvalidOperationException("PermissionService is not registered.");
            }

            _viewModel = new MainWindowViewModel(permissionService, this.MainFrame, this);
            this.DataContext = _viewModel;
            Debug.WriteLine($"MainWindow DataContext set to: {_viewModel.GetType().Name}");

            // 手動觸發屬性更新
            if (_viewModel is MainWindowViewModel vm)
            {
                vm.NotifyPermissionsChanged();
            }

            // 創建 Home 頁面並設置 DataContext
            _homePage = new Home
            {
                DataContext = _serviceProvider.GetService<HomeViewModel>()
            };

            this.Loaded += (s, e) =>
            {
                if (!_viewModel.IsLoggedIn)
                {
                    Debug.WriteLine("Showing LoginWindow from MainWindow.Loaded");
                    //_viewModel.ExecuteShowLogin();
                }
            };

            Debug.WriteLine("MainWindow created.");

        }

        public void ClearNavigationSelection()
        {
            // 直接引用已知的導航按鈕並取消選中
            HomeButton.IsChecked = false;

            // 清空主框架
            MainFrame.Content = null;
        }

        public (bool Success, string Username, string Password) ShowLoginDialog()
        {
            Debug.WriteLine("ShowLoginDialog() called.");
            var loginWindow = new LoginWindow();
            if (this.IsVisible)
            {
                loginWindow.Owner = this;
            }
            bool? result = loginWindow.ShowDialog();
            Debug.WriteLine($"LoginWindow dialog result: {result}");
            // 僅在 DialogResult 為 true 時返回數據
            if (result == true)
            {
                return (true, loginWindow.Username, loginWindow.Password);
            }
            return (false, string.Empty, string.Empty);
        }

        // 首頁
        private void HomeButton_Checked(object sender, RoutedEventArgs e)
        {
            if (_homePage == null)
            {
                _homePage = new Home
                {
                    DataContext = _serviceProvider.GetService<HomeViewModel>()
                };
            }
            MainFrame.Navigate(_homePage);
        }


        // 最小化窗口
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // 關閉窗口
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}