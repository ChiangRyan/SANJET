using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using System.Diagnostics;
using System.Windows;
using SANJET.Core.Services;
using SANJET.UI.Views.Pages;
using SANJET.Core.ViewModels;
using SANJET.Core.Interfaces;
using SANJET.Core.Tools;
using SANJET.UI.Views.Windows;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;

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
                bool isNasAccessible = SqliteDataService.IsPathAccessible(nasPath);
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
            // 配置日誌
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            // 註冊核心服務
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<SqliteDataService>(provider => new SqliteDataService(insertTestData: true));
            services.AddSingleton<PermissionService>();
            services.AddSingleton<ITextToSpeechService, SpeechService>();
            services.AddSingleton<IAudioPlayerService, AudioPlayer>();
            services.AddSingleton<IRecordDialogService, RecordService>(provider =>
                new RecordService(provider.GetRequiredService<SqliteDataService>()));

            // 註冊 LoginDialogService
            services.AddSingleton<ILoginDialogService, LoginDialogService>();

            // 註冊 ViewModels
            services.AddSingleton<MainWindowViewModel>(provider => new MainWindowViewModel(
                provider.GetRequiredService<PermissionService>(),
                provider.GetRequiredService<MainWindow>().MainFrame,
                provider.GetRequiredService<ILoginDialogService>()

            ));

            services.AddSingleton<HomeViewModel>(provider => new HomeViewModel(
                provider.GetRequiredService<ICommunicationService>(),
                provider.GetRequiredService<SqliteDataService>(),
                provider.GetRequiredService<IRecordDialogService>(),
                provider.GetRequiredService<PermissionService>(),
                provider.GetRequiredService<ITextToSpeechService>(),
                provider.GetRequiredService<IAudioPlayerService>(),
                provider.GetRequiredService<ILogger<HomeViewModel>>()
            ));

            // 註冊 MainWindow
            services.AddSingleton<MainWindow>(provider => new MainWindow(
                provider,
                provider.GetRequiredService<MainWindowViewModel>(),
                provider.GetRequiredService<ILogger<MainWindow>>()
            ));

            // 註冊 Home 頁面
            services.AddSingleton<Home>(provider => new Home(
                provider,
                provider.GetRequiredService<ILogger<Home>>()
            ));
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
