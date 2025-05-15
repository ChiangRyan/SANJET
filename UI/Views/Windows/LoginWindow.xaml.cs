using SANJET.Core.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SANJET.UI.Views.Windows
{
    public partial class LoginWindow : Window
    {
        public string Username => (DataContext as LoginViewModel)?.Username;
        public string Password => (DataContext as LoginViewModel)?.Password;

        public LoginWindow()
        {
            InitializeComponent();
            DataContext = new LoginViewModel(this);
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                viewModel.Password = (sender as PasswordBox)?.Password;
            }
        }
    }
}