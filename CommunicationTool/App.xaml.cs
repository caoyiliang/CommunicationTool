using CommunicationTool.ViewModel;
using Config;
using LoggerNLog;
using LogInterface;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CommunicationTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILogger? _logger;
        public new static App Current => (App)Application.Current;
        public IServiceProvider Services { get; private set; } = null!;

        private static async Task<IServiceProvider> ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton(await ConfigManager.Instance.InitAsync());

            //services.AddTransient<IPopupService, PopupService>();

            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();

            //services.AddTransient<ConnectConfig>();
            //services.AddTransient<ConnectConfigViewModel>();

            return services.AddNLog().BuildServiceProvider();

        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            Services = await ConfigureServices();
            Logs.LogFactory = Services.GetRequiredService<ILogFactory>();
            _logger = Logs.LogFactory.GetLogger<App>();

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                var exception = args.Exception;
                _logger?.Error(exception.ToString());
                // 处理未观察到的任务异常
                args.SetObserved();
            };

            var vm = Services.GetRequiredService<MainViewModel>();
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.DataContext = vm;
            MainWindow.Show();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var innerExceptionMessage = e.Exception.InnerException != null ? e.Exception.InnerException.Message : "无内部异常信息";
            MessageBox.Show($"发生未处理的异常: {e.Exception.Message}，内部异常: {innerExceptionMessage}", "异常", MessageBoxButton.OK, MessageBoxImage.Error);
            // 记录异常信息，例如通过日志库或事件记录器
            _logger?.Error(e.Exception);
            // 标记异常为已处理，以防止应用程序终止
            e.Handled = true;
        }
    }

}
