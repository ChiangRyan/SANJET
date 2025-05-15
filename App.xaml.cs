using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;
using System.Diagnostics;
using System.Windows;
using SANJET.SANJET.Core.Services;
using SANJET.UI.Views.Pages;
using SANJET.SANJET.Core.ViewModels;
using SANJET.SANJET.Core.Interfaces;
using SANJET.SANJET.Core.Tools;
using SANJET.SANJET.Core.Models;
using SANJET.UI.Views.Windows;


namespace SANJET
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Debug.WriteLine("Entering OnStartup...");
            try
            {
                Batteries.Init();

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

                var dataService = ServiceProvider.GetService<SqliteDataService>();
                if (dataService == null)
                    throw new InvalidOperationException("SqliteDataService is null...");

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
            
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<SqliteDataService>(provider => new SqliteDataService(insertTestData: true));
            services.AddSingleton<PermissionService>();
            services.AddSingleton<ILoginDialogService, MainWindow>();

            // Add TextToSpeech service registration
            services.AddSingleton<ITextToSpeechService, SpeechService>();
            // Add AudioPlayer service registration
            services.AddSingleton<IAudioPlayerService, AudioPlayer>();
            services.AddSingleton<MainWindow>(provider =>
                new MainWindow(provider.GetService<IServiceProvider>()));

            services.AddSingleton<MainWindowViewModel>(provider =>
                new MainWindowViewModel(
                    provider.GetService<PermissionService>(),
                    provider.GetService<MainWindow>().MainFrame,
                    provider.GetService<MainWindow>()
                ));

            services.AddSingleton<HomeViewModel>(provider =>
                new HomeViewModel(
                    provider.GetService<ICommunicationService>(),
                    provider.GetService<SqliteDataService>(),
                    provider.GetService<IRecordDialogService>(),
                    provider.GetService<PermissionService>(),
                    provider.GetService<ITextToSpeechService>(), // Add TTS service
                    provider.GetService<IAudioPlayerService>() // Add player service
                ));

            services.AddSingleton<IRecordDialogService, RecordService>(provider =>
                new RecordService(provider.GetService<SqliteDataService>()));

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
