using SANJET.Core.Interfaces;
using SANJET.UI.Views.Windows;

namespace SANJET.Core.Services
{
    public class LoginDialogService : ILoginDialogService
    {
        public (bool Success, string Username, string Password) ShowLoginDialog()
        {
            var loginWindow = new LoginWindow();
            bool? result = loginWindow.ShowDialog();
            return result == true
                ? (true, loginWindow.Username, loginWindow.Password)
                : (false, string.Empty, string.Empty);
        }
    }
}