using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using GetVideoService;
using GetVideoService.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("C:\\Logs\\GetVideoService.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting GetVideoService...");
    
    var builder = Host.CreateApplicationBuilder(args);
    
    // Add Windows Service support
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "GetVideoService";
    });

    // Add Serilog
    builder.Services.AddSerilog();

    // Register the worker service
    builder.Services.AddHostedService<VideoDownloadWorker>();
    
    // Register event notifier service
    builder.Services.AddSingleton<IServiceEventNotifier, ServiceEventNotifier>();

    // Add configuration
    builder.Services.Configure<ServiceSettings>(builder.Configuration.GetSection("ServiceSettings"));

    var host = builder.Build();
    
    Log.Information("Host built successfully, starting...");
    await host.RunAsync();
}
catch (OperationCanceledException)
{
    // This is expected during service shutdown, log as info
    Log.Information("Service shutdown completed");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw; // Re-throw to ensure service fails properly if there's an issue
}
finally
{
    Log.CloseAndFlush();
}
