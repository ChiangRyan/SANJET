using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;
using System.Diagnostics;
using System.Windows;
using SANJET.Core.Services;
using SANJET.UI.Views.Pages;
using SANJET.Core.ViewModels;
using SANJET.Core.Interfaces;
using SANJET.Core.Tools;



namespace SANJET
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Debug.WriteLine("Entering OnStartup...");
            try
            {
                // 根據 Microsoft.Data.Sqlite 的文件，對於 .NET Core / .NET 5+，
                // 通常不再需要手動呼叫 Batteries.Init()，除非有特定原因。
                // 如果您的 SQLitePCL.raw 版本較舊或有特殊設定，可能仍需要。
                // 建議測試移除它，看看是否仍然工作正常。
                // SQLitePCL.Batteries.Init(); // 或更新為 Batteries_V2.Init();

                Debug.WriteLine("SQLite provider initialized (if Batteries.Init was called).");

                base.OnStartup(e);
                Debug.WriteLine("base.OnStartup called successfully.");

                var services = new ServiceCollection();
                Debug.WriteLine("Adding services to DI container...");

                ConfigureServices(services);
                Debug.WriteLine("Services added to DI container.");
                Debug.WriteLine("Building service provider...");

                ServiceProvider = services.BuildServiceProvider();

                Debug.WriteLine("Service provider built successfully.");

                var loadingWindow = new LoadingWindow();
                loadingWindow.Show();
                Debug.WriteLine("LoadingWindow shown.");

                // 使用 GetRequiredService 以確保服務存在，並處理可能的 null 情況
                var dataService = ServiceProvider.GetRequiredService<SqliteDataService>();
                string nasPath = @"\\192.168.88.3\電控工程課\107_姜集翔\SANJET\SJ_data.db";
                Debug.WriteLine($"Checking NAS path accessibility: {nasPath}");
                bool isNasAccessible = dataService.IsPathAccessible(nasPath);
                if (!isNasAccessible)
                {
                    Debug.WriteLine($"NAS path {nasPath} is not accessible. Switching to local path.");
                    dataService.SetDatabasePath("SJ_data.db");
                    Debug.WriteLine("Local database path set: SJ_data.db");
                }
                else
                {
                    dataService.SetDatabasePath(nasPath);
                    Debug.WriteLine($"NAS database path set: {nasPath}");
                }

                Dispatcher.Invoke(async () =>
                {
                    var mainWindow = ServiceProvider.GetService<MainWindow>();
                    if (mainWindow == null)
                        throw new InvalidOperationException("MainWindow is null...");
                    mainWindow.DataContext = ServiceProvider.GetService<MainWindowViewModel>();
                    Debug.WriteLine("MainWindow DataContext set.");

                    await Task.Delay(2000);

                    loadingWindow.Close();
                    Debug.WriteLine("LoadingWindow closed.");

                    if (!isNasAccessible)
                    {
                        MessageBox.Show(
                            "無法連接 NAS 路徑: " + nasPath + "\n將使用本地路徑作為備用。",
                            "連線錯誤",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }

                    mainWindow.Show();
                    Debug.WriteLine("MainWindow shown.");

                    var viewModel = mainWindow.DataContext as MainWindowViewModel;
                    if (viewModel != null && !viewModel.IsLoggedIn)
                    {
                        Debug.WriteLine("Showing LoginWindow from App.OnStartup");
                        viewModel.ShowLogin();
                    }
                });

                Debug.WriteLine("Application startup completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Application startup failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"應用程式啟動失敗：{ex.Message}\n\n詳細資訊：{ex}",
                        "錯誤",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    Shutdown();
                });
            }
        }


        private void ConfigureServices(IServiceCollection services)
        {
            // 註冊核心服務
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<SqliteDataService>(provider => new SqliteDataService(insertTestData: true));
            services.AddSingleton<PermissionService>();
            services.AddSingleton<ITextToSpeechService, SpeechService>();
            services.AddSingleton<IAudioPlayerService, AudioPlayer>();
            services.AddSingleton<IRecordDialogService, RecordService>(provider =>
                new RecordService(provider.GetRequiredService<SqliteDataService>()));

            // 註冊 MainWindow
            // MainWindow 自身通常不需要在建構函式中注入 IServiceProvider，
            // 除非有特殊需求。若無，保持其無參數建構函式。
            services.AddSingleton<MainWindow>();

            // 將已註冊的 MainWindow 實例同時作為 ILoginDialogService 的實現提供
            services.AddSingleton<ILoginDialogService>(provider =>
                provider.GetRequiredService<MainWindow>());

            // 註冊 ViewModels
            services.AddSingleton<MainWindowViewModel>(provider =>
            {
                // 獲取已創建的 MainWindow 實例
                var mainWindowInstance = provider.GetRequiredService<MainWindow>();
                return new MainWindowViewModel(
                    provider.GetRequiredService<PermissionService>(),
                    mainWindowInstance.MainFrame, // 從 MainWindow 實例獲取 MainFrame
                    mainWindowInstance  // mainWindowInstance 已實現 ILoginDialogService
                );
            });

            services.AddSingleton<HomeViewModel>(provider =>
                new HomeViewModel(
                    provider.GetRequiredService<ICommunicationService>(),
                    provider.GetRequiredService<SqliteDataService>(),
                    provider.GetRequiredService<IRecordDialogService>(),
                    provider.GetRequiredService<PermissionService>(),
                    provider.GetRequiredService<ITextToSpeechService>(),
                    provider.GetRequiredService<IAudioPlayerService>()
                ));

            // LoginViewModel 通常與 LoginWindow 關聯，如果 LoginWindow 是臨時創建的，
            // LoginViewModel 可能不需要註冊為 Singleton，除非有特定共享需求。
            // 如果 LoginViewModel 是由 LoginWindow 內部創建和使用的，則無需在此處註冊。
        }


        protected override void OnExit(ExitEventArgs e)
        {
            Debug.WriteLine("Application exiting...");
            try
            {
                var communicationService = ServiceProvider?.GetService<ICommunicationService>();
                if (communicationService != null)
                {
                    Debug.WriteLine("Cleaning up connections...");
                    communicationService.CleanupConnections();
                    Debug.WriteLine("Connections cleaned up.");
                }

                foreach (Window window in Application.Current.Windows)
                {
                    if (window != null)
                    {
                        Debug.WriteLine($"Closing window: {window.GetType().Name}");
                        window.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnExit failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            base.OnExit(e);
        }
    }
}
