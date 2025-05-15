using CommunityToolkit.Mvvm.ComponentModel; // 引入 MVVM Toolkit 的 ObservableObject
using CommunityToolkit.Mvvm.Input;      // 引入 MVVM Toolkit 的 RelayCommand
using System.Windows;                   // 需要 Window 和 DialogResult

namespace SANJET.Core.ViewModels // 假設這是您 ViewModel 的正確命名空間
{
    public partial class LoginViewModel : ObservableObject // 繼承自 ObservableObject
    {
        private readonly Window _window; // 用於控制傳入的視窗

        // 使用 [ObservableProperty] 自動生成 Username 屬性及其變更通知
        // 直接在這裡初始化欄位值
        [ObservableProperty]
        private string _username = string.Empty;

        // 使用 [ObservableProperty] 自動生成 Password 屬性及其變更通知
        [ObservableProperty]
        private string _password = string.Empty;

        // 建構函數
        public LoginViewModel(Window window)
        {
            _window = window;
            // Username 和 Password 欄位已在上方初始化。
            // 不需要在此處初始化命令，它們將由 [RelayCommand] 屬性自動生成。
        }

        // Login 方法將通過 [RelayCommand] 屬性自動生成名為 LoginCommand 的命令
        [RelayCommand]
        private void Login()
        {
            // 實際的登入邏輯 (身份驗證) 通常是通過呼叫服務 (Service)
            // 或其他 ViewModel 的方法來處理。
            // 根據原始程式碼，此 ViewModel 的職責是收集憑證並向 LoginWindow 發出成功/失敗的信號。
            _window.DialogResult = true; // 向 LoginWindow 發出成功信號
            _window.Close();             // 關閉視窗
        }

        // Cancel 方法將通過 [RelayCommand] 屬性自動生成名為 CancelCommand 的命令
        [RelayCommand]
        private void Cancel()
        {
            // 可選擇清除憑證，但因為視窗即將關閉，這通常不是必需的。
            // Username = string.Empty; // 這裡會使用自動生成的公開 Username 屬性
            // Password = string.Empty; // 這裡會使用自動生成的公開 Password 屬性
            _window.DialogResult = false; // 向 LoginWindow 發出取消/失敗信號
            _window.Close();              // 關閉視窗
        }
    }
}