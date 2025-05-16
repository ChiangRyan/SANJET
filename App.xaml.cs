using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using SANJET.Core.Services;
using SANJET.UI.Views.Pages;
using SANJET.Core.ViewModels;
using SANJET.Core.Interfaces;
using SANJET.Core.Tools;
using SANJET.UI.Views.Windows;

namespace SANJET
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {
            Debug.WriteLine("Entering OnStartup...");
            try
            {
                // 移除 Batteries.Init()，除非必要
                // SQLitePCL.Batteries.Init();
                Debug.WriteLine("SQLite provider initialized.");

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

                var dataService = ServiceProvider.GetRequiredService<SqliteDataService>();
                string nasPath = @"\\192.168.88.3\電控工程課\107_姜集翔\SANJET\SJ_data.db";
                Debug.WriteLine($"Checking NAS path accessibility: {nasPath}");

                // 非同步檢查 NAS 路徑
                bool isNasAccessible = await Task.Run(() => SqliteDataService.IsPathAccessible(nasPath));
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

                // 將 UI 邏輯移到非同步方法
                await InitializeMainWindowAsync(loadingWindow, isNasAccessible, nasPath);
                Debug.WriteLine("Application startup completed.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Application startup failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await Dispatcher.InvokeAsync(() =>
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

        private async Task InitializeMainWindowAsync(Window loadingWindow, bool isNasAccessible, string nasPath)
        {
            try
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>();
                Debug.WriteLine("MainWindow DataContext set.");

                await Task.Delay(2000); // 模擬載入延遲

                await Dispatcher.InvokeAsync(() =>
                {
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
                });

                var viewModel = mainWindow.DataContext as MainWindowViewModel;
                if (viewModel != null && !viewModel.IsLoggedIn)
                {
                    Debug.WriteLine("Showing LoginWindow from App.OnStartup");
                    await Dispatcher.InvokeAsync(() => viewModel.ShowLogin());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeMainWindowAsync failed: {ex.Message}\nStackTrace: {ex.StackTrace}");
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"初始化主視窗失敗：{ex.Message}\n\n詳細資訊：{ex}",
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
            services.AddLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            });

            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<SqliteDataService>(provider => new SqliteDataService(insertTestData: true));
            services.AddSingleton<PermissionService>(provider => new PermissionService(
                provider.GetRequiredService<SqliteDataService>(),
                provider.GetRequiredService<ILogger<PermissionService>>()
            ));
            services.AddSingleton<ITextToSpeechService, SpeechService>();
            services.AddSingleton<IAudioPlayerService, AudioPlayerService>(provider => new AudioPlayerService(
                provider.GetRequiredService<ILogger<AudioPlayerService>>()
            ));
            services.AddSingleton<IRecordDialogService, RecordService>(provider =>
                new RecordService(provider.GetRequiredService<SqliteDataService>()));

            services.AddSingleton<LoginViewModel>();
            services.AddSingleton<ILoginWindowFactory, LoginWindowFactory>();
            services.AddSingleton<ILoginDialogService>(provider => new LoginDialogService(
                provider.GetRequiredService<ILoginWindowFactory>(),
                provider.GetRequiredService<ILogger<LoginDialogService>>()
            ));

            // 修改 MainWindow 和 MainWindowViewModel 的註冊，避免循環依賴
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>(provider => new MainWindowViewModel(
                provider.GetRequiredService<PermissionService>(),
                provider.GetRequiredService<MainWindow>().MainFrame,
                provider.GetRequiredService<ILoginDialogService>(),
                provider.GetRequiredService<HomeViewModel>(),
                provider.GetRequiredService<ILogger<MainWindowViewModel>>(),
                provider
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