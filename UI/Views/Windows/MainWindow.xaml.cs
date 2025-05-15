// MainWindow.xaml.cs
using SANJET.Core.Interfaces;
using SANJET.UI.Views.Windows;
using System.Windows;


public partial class MainWindow : Window, ILoginDialogService // <--- 添加介面實現
{
    // ...
    // 您需要實現 ILoginDialogService 的所有方法和屬性
    // 例如：
    public (bool Success, string Username, string Password) ShowLoginDialog()
    {
        var loginWindow = new LoginWindow(); // LoginWindow 應該顯示並返回結果
        if (loginWindow.ShowDialog() == true)
        {
            return (true, loginWindow.Username, loginWindow.Password);
        }
        return (false, string.Empty, string.Empty);
    }

    public void ClearNavigationSelection()
    {
        // 實現清除導航選擇的邏輯，例如：
        // NavListBox.SelectedItem = null; (如果您的 MainWindow 中有這樣的控制項)
    }
}