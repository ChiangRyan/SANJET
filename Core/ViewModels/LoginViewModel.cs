using System.Windows;
using System.Windows.Input;

namespace SANJET.Core.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly Window _window;
        private string _username;
        private string _password;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(nameof(Username)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public ICommand LoginCommand { get; }
        public ICommand CancelCommand { get; }

        public LoginViewModel(Window window)
        {
            _window = window;
            _username = string.Empty;
            _password = string.Empty;

            LoginCommand = new RelayCommand(() =>
            {
                _window.DialogResult = true;
                _window.Close();
            });

            CancelCommand = new RelayCommand(() =>
            {
                Username = string.Empty;
                Password = string.Empty;
                _window.DialogResult = false;
                _window.Close();
            });
        }
    }
}
