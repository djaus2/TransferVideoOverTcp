using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using GetVideoNow.ViewModels;
using Microsoft.Extensions.Configuration;
using System.IO;
using GetVideoNow.Services;

namespace GetVideoNow
{
    public partial class App : System.Windows.Application
    {
        private ServiceProvider _serviceProvider;
        public IConfiguration Configuration { get; private set; }

        public App()
        {
            // Initialize Configuration in constructor to fix CS8618 warning
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
            
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Register services
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IVideoDownloadService, VideoDownloadService>();
            services.AddSingleton<MainWindowViewModel>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create main window with view model from DI
            var mainWindow = new MainWindow();
            var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            mainWindow.DataContext = viewModel;
            mainWindow.Show();
        }
    }
}
