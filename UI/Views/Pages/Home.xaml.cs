
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SANJET.SANJET.Core.ViewModels;

namespace SANJET.UI.Views.Pages
{
    public partial class Home : Page
    {
        private readonly HomeViewModel _viewModel;

        public Home()
        {
            InitializeComponent();

            var app = (App)Application.Current;
            _viewModel = app.ServiceProvider.GetService<HomeViewModel>();
            DataContext = _viewModel;

            Loaded += Home_Loaded;
            Unloaded += Home_Unloaded;
        }

        private void Home_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StartPolling();// 當頁面載入時，啟動輪巡（如果需要）
            System.Diagnostics.Debug.WriteLine("Update timer started due to page load.");
        }

        private void Home_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.StopPolling();
        }
    }
}