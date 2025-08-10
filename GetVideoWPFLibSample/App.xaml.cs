using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows;
using GetVideoWPFLib.Services;
using GetVideoWPFLibSample.ViewModels;

// Add alias to avoid ambiguity between System.Windows.Forms.Application and System.Windows.Application
using WpfApplication = System.Windows.Application;

namespace GetVideoWPFLibSample
{
    public partial class App : WpfApplication
    {
        private ServiceProvider _serviceProvider;
        public IConfiguration Configuration { get; private set; }

        public App()
        {
            // Initialize Configuration
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
            // Register configuration
            services.AddSingleton<IConfiguration>(Configuration);
            
            // Register GetVideoWPFLib services
            services.AddSingleton<IVideoDownloadService, VideoDownloadService>();
            
            // Register view models
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
